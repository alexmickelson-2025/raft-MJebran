using test;
namespace logic;

public enum NodeState { Follower, Candidate, Leader }

public class RaftNode : IRaftNode
{
    public Guid Id { get; set; }
    public NodeState State { get; set; }
    public Guid? CurrentLeaderId { get; set; }
    public int CurrentTerm { get; set; }
    public Guid? VotedFor { get; set; }
    public int ElectionTimeout { get; private set; }
    public DateTime LastHeartbeat;
    private MockCluster? _cluster;
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    public List<LogEntry> Log { get; private set; } = new List<LogEntry>();
    public int CommitIndex { get; private set; }
    private Dictionary<Guid, int> nextIndex = new Dictionary<Guid, int>();
    public Action<LogEntry>? OnApplyLogEntry { get; set; }
    private Dictionary<int, int> acknowledgmentCounts = new Dictionary<int, int>();



    public void SetCluster(MockCluster cluster)
    {
        _cluster = cluster;
    }

    public RaftNode()
    {
        Id = Guid.NewGuid();
        State = NodeState.Follower;
        CurrentTerm = 0;
        CurrentLeaderId = null;
        VotedFor = null;
        ResetElectionTimer();
        CancellationTokenSource = new CancellationTokenSource();
        OtherNodes = new List<IRaftNode>();
        HeartbeatTimer = null;
        OnHeartbeat = () => { };

    }

    public NodeState GetState() => State;

    public void BecomeCandidate()
    {
        State = NodeState.Candidate;
        CurrentTerm++;
        VotedFor = Id;
    }

    public void BecomeFollower()
    {
        State = NodeState.Follower;
        VotedFor = null;
    }

    public int GetVoteCount() => VotedFor == Id ? 1 : 0;

    public bool HasVotedFor(Guid candidateId) => VotedFor == candidateId;

    public void HandleAppendEntries(AppendEntriesRPCDTO appendEntries)
    {
        if (appendEntries.Term > CurrentTerm)
        {
            CurrentTerm = appendEntries.Term;
            State = NodeState.Follower;
            CurrentLeaderId = appendEntries.LeaderId;
        }
        else if (appendEntries.Term == CurrentTerm)
        {
            State = NodeState.Follower;
            CurrentLeaderId = appendEntries.LeaderId;
        }

        foreach (var entry in appendEntries.Entries)
        {
            if (!Log.Any(existingEntry => existingEntry.Term == entry.Term && existingEntry.Command == entry.Command))
            {
                Log.Add(entry);
            }
        }

        ResetElectionTimer();
    }

    public AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPCDTO rpc)
    {
        if (rpc.Term < CurrentTerm)
        {
            return new AppendEntriesResponse { Success = false };
        }

        if (rpc.Entries.Count > 0 && Log.Count > 0)
        {
            var lastEntry = Log.Last();
            var newEntry = rpc.Entries.First();

            if (lastEntry.Term != newEntry.Term)
            {
                Console.WriteLine($"Rejected AppendEntries: Log inconsistency (Expected term {lastEntry.Term}, got {newEntry.Term})");
                return new AppendEntriesResponse { Success = false };
            }
        }

        HandleAppendEntries(rpc);

        if (rpc.CommitIndex > CommitIndex)
        {
            CommitIndex = rpc.CommitIndex;
            ApplyCommittedEntries();
        }

        return new AppendEntriesResponse { Success = true };
    }

    public RequestForVoteResponse HandleRequestForVote(RequestForVoteRPCDTO rpc)
    {
        if (rpc.Term < CurrentTerm)
        {
            return new RequestForVoteResponse { VoteGranted = false };
        }

        if (rpc.Term > CurrentTerm || VotedFor == null)
        {
            CurrentTerm = rpc.Term;
            VotedFor = rpc.CandidateId;
            return new RequestForVoteResponse { VoteGranted = true };
        }

        return new RequestForVoteResponse { VoteGranted = false };
    }

    public void ResetElectionTimer()
    {
        var random = new Random();
        ElectionTimeout = random.Next(150, 301);
        LastHeartbeat = DateTime.UtcNow;
    }

    public void StartElection()
    {
        State = NodeState.Candidate;
        CurrentTerm++;
        VotedFor = Id;
        VotesReceived = 1;
        foreach (var node in OtherNodes)
        {
            var voteRequest = new RequestForVoteRPCDTO(CurrentTerm, Id);
            var voteResponse = node.HandleRequestForVote(voteRequest);
            if (voteResponse.VoteGranted)
            {
                ReceiveVote();
            }
        }

        if (HasMajorityVotes(OtherNodes.Count + 1))
        {
            BecomeLeader();
        }
    }

    public void RunElectionLoop()
    {
        CancellationTokenSource ??= new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                CheckElectionTimeout();

                if (State == NodeState.Leader)
                {
                    Console.WriteLine($"Leader {Id} sending heartbeats.");
                    SendHeartbeat();
                }

                await Task.Delay(50);
            }
        }, CancellationTokenSource.Token);
    }

    public void PauseElectionLoop()
    {
        Console.WriteLine("Pausing election loop...");
        if (CancellationTokenSource != null)
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            CancellationTokenSource = null;
        }
        StopHeartbeatTimer();
    }

    public void UnpauseElectionLoop()
    {
        Console.WriteLine("Unpausing election loop...");
        if (CancellationTokenSource == null || CancellationTokenSource.Token.IsCancellationRequested)
        {
            CancellationTokenSource = new CancellationTokenSource();
            RunElectionLoop();
            StartHeartbeatTimer(100);
            ResetElectionTimer();
        }
    }

    public void StartElectionTimer(int timeoutMs)
    {
        LastHeartbeat = DateTime.UtcNow.AddMilliseconds(-timeoutMs);
    }

    public void CheckElectionTimeout()
    {
        if (CancellationTokenSource == null || CancellationTokenSource.Token.IsCancellationRequested)
        {
            return;
        }

        Console.WriteLine($"Checking election timeout for node {Id} in state {State}...");
        if ((DateTime.UtcNow - LastHeartbeat).TotalMilliseconds > ElectionTimeout)
        {
            Console.WriteLine($"Election timeout reached for node {Id} in state {State}...");
            if (State == NodeState.Follower)
            {
                StartElection();
            }
            else if (State == NodeState.Candidate && OtherNodes.Any())
            {
                CurrentTerm++;
                VotesReceived = 1;
                foreach (var node in OtherNodes)
                {
                    var voteRequest = new RequestForVoteRPCDTO(CurrentTerm, Id);
                    var voteResponse = node.HandleRequestForVote(voteRequest);
                    if (voteResponse.VoteGranted)
                    {
                        ReceiveVote();
                    }
                }

                if (HasMajorityVotes(OtherNodes.Count + 1))
                {
                    BecomeLeader();
                }
            }
        }
    }

    private int VotesReceived { get; set; } = 0;

    public void ReceiveVote()
    {
        VotesReceived++;
    }

    public bool HasMajorityVotes(int totalNodes)
    {
        return VotesReceived > totalNodes / 2;
    }

    private System.Timers.Timer? HeartbeatTimer { get; set; }

    public Action OnHeartbeat { get; set; }

    public List<IRaftNode> OtherNodes { get; set; }

    public void StartHeartbeatTimer(int intervalMs)
    {
        if (State != NodeState.Leader) return;

        Console.WriteLine("Starting heartbeat timer...");
        HeartbeatTimer = new System.Timers.Timer(intervalMs);
        HeartbeatTimer.Elapsed += (sender, e) =>
        {
            SendHeartbeat();
        };
        HeartbeatTimer.AutoReset = true;
        HeartbeatTimer.Start();
    }

    public void SendHeartbeat()
    {
        if (State != NodeState.Leader) return;

        Console.WriteLine($"Leader {Id} sending heartbeats.");
        foreach (var node in OtherNodes)
        {
            var uncommittedEntries = Log.Skip(CommitIndex).ToList();
            var heartbeat = new AppendEntriesRPCDTO(Id, CurrentTerm, uncommittedEntries, CommitIndex);
            node.HandleAppendEntries(heartbeat);
        }
    }

    public void StopHeartbeatTimer()
    {
        if (HeartbeatTimer != null)
        {
            Console.WriteLine("Stopping heartbeat timer...");
            HeartbeatTimer.Stop();
            HeartbeatTimer.Dispose();
            HeartbeatTimer = null;
        }
    }

    public void BecomeLeader()
    {
        State = NodeState.Leader;
        int lastLogIndex = Log.Count;
        foreach (var follower in OtherNodes)
        {
            nextIndex[follower.Id] = lastLogIndex + 1;
        }
        SendHeartbeat();
    }

    public bool HasNextIndexForFollower(Guid followerId)
    {
        return nextIndex.ContainsKey(followerId);
    }

    public int GetNextIndexForFollower(Guid followerId)
    {
        return nextIndex.ContainsKey(followerId) ? nextIndex[followerId] : -1;
    }

    public void UpdateNextIndexForFollower(Guid followerId, int newIndex)
    {
        if (nextIndex.ContainsKey(followerId))
        {
            nextIndex[followerId] = newIndex;
        }
    }

    public void SetCommitIndexForTesting(int value)
    {
        CommitIndex = value;
    }

    public void ApplyCommittedEntries()
    {
        for (int i = 0; i <= CommitIndex && i < Log.Count; i++)
        {
            var logEntry = Log[i];
            if (OnApplyLogEntry != null)
            {
                Console.WriteLine($"Applying log entry: {logEntry.Command} at index {i}");
                OnApplyLogEntry(logEntry);
            }
            else
            {
                Console.WriteLine("OnApplyLogEntry is null, cannot apply log entry!");
            }
        }
    }

    public void ReceiveAppendEntriesAck(Guid followerId, LogEntry logEntry)
    {
        int logIndex = Log.FindIndex(entry => entry.Command == logEntry.Command && entry.Term == logEntry.Term);

        if (logIndex == -1)
        {
            Console.WriteLine($"Log entry not found for acknowledgment: {logEntry.Command}");
            return;
        }

        if (!acknowledgmentCounts.ContainsKey(logIndex))
        {
            acknowledgmentCounts[logIndex] = 1;
        }
        else
        {
            acknowledgmentCounts[logIndex]++;
        }

        Console.WriteLine($"Acknowledgment for log entry {logIndex}: {acknowledgmentCounts[logIndex]}");

        int majority = (OtherNodes.Count + 1) / 2;
        if (acknowledgmentCounts[logIndex] >= majority)
        {
            CommitIndex = logIndex + 1;
            Console.WriteLine($"CommitIndex updated to {CommitIndex}");
            SendConfirmationToClient(logEntry);
        }
    }

    private void SendConfirmationToClient(LogEntry logEntry)
    {
        Console.WriteLine($"Leader {Id} confirming log replication: {logEntry.Command}");
    }

    public void UpdateCommitIndex()
    {
        for (int i = CommitIndex + 1; i <= Log.Count; i++)
        {
            int ackCount = OtherNodes.Count(follower => acknowledgmentCounts.ContainsKey(i) && acknowledgmentCounts[i] > 0);

            if (ackCount + 1 > (OtherNodes.Count + 1) / 2)
            {
                CommitIndex = i;
                Console.WriteLine($"CommitIndex updated to {CommitIndex}");
            }
            else
            {   // easy to read for me
                Console.WriteLine($"Entry at index {i} remains uncommitted (Ack count: {ackCount})");
            }
        }
        ApplyCommittedEntries();
    }

    public void SendCommand(ClientCommandData command)
    {
        if (State == NodeState.Leader)
        {
            Console.WriteLine($"Leader {Id} processing command: {command.Type} {command.Key} = {command.Value}");
            // command.RespondToClient(true, Id);

            var logEntry = new LogEntry(CurrentTerm, $"{command.Type} {command.Key}={command.Value}");
            Log.Add(logEntry);

            var appendEntriesRpc = new AppendEntriesRPCDTO(Id, CurrentTerm, new List<LogEntry> { logEntry }, CommitIndex);

            foreach (var follower in OtherNodes ?? new List<IRaftNode>())
            {
                follower.HandleAppendEntries(appendEntriesRpc);
            }
            Task.Run(async () =>
            {
                await Task.Delay(500);
                UpdateCommitIndex();
                if (CommitIndex >= Log.IndexOf(logEntry))
                {
                    Console.WriteLine($"Leader {Id} confirming command execution.");
                    command.RespondToClient(true, Id);
                }
                else
                {
                    Console.WriteLine($"Leader {Id} could not commit entry, not responding.");
                }
            });
        }
        else if (CurrentLeaderId.HasValue)
        {
            Console.WriteLine($"Node {Id} forwarding command to leader {CurrentLeaderId}");
            command.RespondToClient(false, CurrentLeaderId);
        }
        else
        {
            Console.WriteLine($"Node {Id} unable to process command: No leader");
            command.RespondToClient(false, null);
        }
    }



}
