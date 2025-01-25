﻿using test;
namespace logic;

public enum NodeState { Follower, Candidate, Leader }

public class RaftNode : IRaftNode
{
    public Guid Id { get; private set; }
    public NodeState State { get; set; }
    public Guid? CurrentLeaderId { get; set; }
    public int CurrentTerm { get; set; }
    public Guid? VotedFor { get; set; }
    public int ElectionTimeout { get; private set; }
    public DateTime LastHeartbeat;
    private MockCluster? _cluster;
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public List<LogEntry> Log { get; private set; } = new List<LogEntry>();
    private int CommitIndex { get; set; }
    private Dictionary<Guid, int> nextIndex = new Dictionary<Guid, int>();


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

    public int GetVoteCount() => VotedFor == Id ? 1 : 0;

    public bool HasVotedFor(Guid candidateId) => VotedFor == candidateId;

    public void HandleAppendEntries(AppendEntriesRPC appendEntries)
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
        ResetElectionTimer();
    }


    public AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPC rpc)
    {
        if (rpc.Term < CurrentTerm)
        {
            return new AppendEntriesResponse { Success = false };
        }
        HandleAppendEntries(rpc);
        return new AppendEntriesResponse { Success = true };
    }

    public RequestForVoteResponse HandleRequestForVote(RequestForVoteRPC rpc)
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
            var voteRequest = new RequestForVoteRPC(CurrentTerm, Id);
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
        Task.Run(async () =>
        {
            while (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                CheckElectionTimeout();
                await Task.Delay(50);
            }
        }, CancellationTokenSource.Token);
    }

    public void StartElectionTimer(int timeoutMs)
    {
        LastHeartbeat = DateTime.UtcNow.AddMilliseconds(-timeoutMs);
    }

    public void CheckElectionTimeout()
    {
        if ((DateTime.UtcNow - LastHeartbeat).TotalMilliseconds > ElectionTimeout)
        {
            if (State == NodeState.Follower)
            {
                StartElection();
            }
            else if (State == NodeState.Candidate)
            {
                CurrentTerm++;
                VotesReceived = 1;
                foreach (var node in OtherNodes)
                {
                    var voteRequest = new RequestForVoteRPC(CurrentTerm, Id);
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

    private System.Timers.Timer HeartbeatTimer { get; set; }


    public Action OnHeartbeat { get; set; }

    public List<IRaftNode> OtherNodes { get; set; }

    public void StartHeartbeatTimer(int intervalMs)
    {
        if (State != NodeState.Leader) return;
        HeartbeatTimer = new System.Timers.Timer(intervalMs);
        HeartbeatTimer.Elapsed += (sender, e) =>
        {
            SendHeartbeat();
        };
        HeartbeatTimer.AutoReset = true;
        HeartbeatTimer.Start();
    }

    private void SendHeartbeat()
    {
        if (State != NodeState.Leader) return;

        foreach (var node in OtherNodes)
        {
            var heartbeat = new AppendEntriesRPC(Id, CurrentTerm, new List<LogEntry>());
            node.ProcessAppendEntries(heartbeat);
        }
    }

    public void StopHeartbeatTimer()
    {
        if (HeartbeatTimer != null)
        {
            HeartbeatTimer.Stop();
            HeartbeatTimer.Dispose();
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

    public int GetNextIndexForFollower(Guid followerId)
    {
        return nextIndex.ContainsKey(followerId) ? nextIndex[followerId] : -1;
    }

    public void SendCommand(ClientCommandData command)
    {
        if (State == NodeState.Leader)
        {
            Console.WriteLine($"Leader {Id} processing command: {command.Type} {command.Key} = {command.Value}");
            command.RespondToClient(true, Id);

            var logEntry = new LogEntry(CurrentTerm, $"{command.Type} {command.Key}={command.Value}");
            Log.Add(logEntry);

            var appendEntriesRpc = new AppendEntriesRPC(Id, CurrentTerm, new List<LogEntry> { logEntry });

            foreach (var follower in OtherNodes ?? new List<IRaftNode>())
            {
                follower.HandleAppendEntries(appendEntriesRpc);
            }

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
