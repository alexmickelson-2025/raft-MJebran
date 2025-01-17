using test;

namespace logic;



public enum NodeState
{
    Follower,
    Candidate,
    Leader
}

public class RaftNode : IRaftNode
{
    public Guid Id { get; private set; }
    public NodeState State { get; set; }
    public Guid? CurrentLeaderId { get; private set; }
    public int CurrentTerm { get; set; }
    public Guid? VotedFor { get; set; }
    public int ElectionTimeout { get; private set; }
    private DateTime LastHeartbeat;
    private MockCluster? _cluster;

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
    }

    // Test #1: Node Initialization
    public NodeState GetState() => State;

    // Test #2: Candidate Self Voting
    public void BecomeCandidate()
    {
        State = NodeState.Candidate;
        CurrentTerm++;
        VotedFor = Id;
    }

    public int GetVoteCount() => VotedFor == Id ? 1 : 0;

    public bool HasVotedFor(Guid candidateId) => VotedFor == candidateId;

    // Test #3: Leader Recognition
    public void HandleAppendEntries(AppendEntriesRPC appendEntries)
    {
        // If the incoming term is greater, update the term, state, and leader
        if (appendEntries.Term > CurrentTerm)
        {
            CurrentTerm = appendEntries.Term; // Update term
            State = NodeState.Follower; // Transition to Follower state
            CurrentLeaderId = appendEntries.LeaderId; // Update the current leader
        }
        else if (appendEntries.Term == CurrentTerm)
        {
            // If the term is the same, remain a follower and update leader ID
            State = NodeState.Follower; // Ensure the node stays a follower
            CurrentLeaderId = appendEntries.LeaderId; // Update the current leader
        }

        ResetElectionTimer();

        // Reset the heartbeat timer
        LastHeartbeat = DateTime.UtcNow; //Reset 
    }



    // Test #4: AppendEntries Response
    public AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPC rpc)
    {
        if (rpc.Term < CurrentTerm)
        {
            return new AppendEntriesResponse { Success = false };
        }
        HandleAppendEntries(rpc);
        return new AppendEntriesResponse { Success = true };
    }

    // Test #5: Voting for Previous Term
    public RequestForVoteResponse HandleRequestForVote(RequestForVoteRPC rpc)
    {
        if (rpc.Term < CurrentTerm)
        {
            return new RequestForVoteResponse { VoteGranted = false };
        }

        if (rpc.Term > CurrentTerm)
        {
            CurrentTerm = rpc.Term;
            VotedFor = rpc.CandidateId;
            return new RequestForVoteResponse { VoteGranted = true };
        }

        // If the term is the same and the node hasn't voted, grant the vote
        if (rpc.Term == CurrentTerm && VotedFor == null)
        {
            VotedFor = rpc.CandidateId;
            return new RequestForVoteResponse { VoteGranted = true };
        }

        // If the term is the same and the node has already voted, reject
        if (rpc.Term == CurrentTerm && VotedFor != null && VotedFor != rpc.CandidateId)
        {
            return new RequestForVoteResponse { VoteGranted = false };
        }

        return new RequestForVoteResponse { VoteGranted = false };
    }


    // Test #6: Randomized Election Timeout
    public void ResetElectionTimer()
    {
        var random = new Random();
        ElectionTimeout = random.Next(150, 301);
    }

    // Test #7: Term Increment on Election Start
    public void StartElection()
    {
        BecomeCandidate();
    }

    // Test #9: Election Timer Expiry
    public void StartElectionTimer(int timeoutMs)
    {
        LastHeartbeat = DateTime.UtcNow.AddMilliseconds(-timeoutMs);
    }

    public void CheckElectionTimeout()
    {
        if ((DateTime.UtcNow - LastHeartbeat).TotalMilliseconds > ElectionTimeout)
        {
            if (State == NodeState.Follower || State == NodeState.Candidate)
            {
                StartElection();
            }
        }
    }
    // Test #10: Heartbeat Timer Start
    private int VotesReceived { get; set; } = 0;

    public void ReceiveVote()
    {
        VotesReceived++;
    }

    public bool HasMajorityVotes(int totalNodes)
    {
        return VotesReceived > totalNodes / 2;
    }

    // Test #11: Heartbeat Timer Stop
    private System.Timers.Timer HeartbeatTimer { get; set; }


    public Action OnHeartbeat { get; set; }

    public void StartHeartbeatTimer(int intervalMs)
    {
        if (State != NodeState.Leader) return; // Only leaders send heartbeats
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
        OnHeartbeat?.Invoke();

        Console.WriteLine("Heartbeat sent to followers.");
    }

    //private void SendHeartbeat()
    // {
    //     if (_cluster != null)
    //     {
    //         var heartbeat = new AppendEntriesRPC(
    //             leaderId: Id,
    //             term: CurrentTerm,
    //             entries: new List<LogEntry>()
    //         );
    //         _cluster.SendHeartbeat(heartbeat);
    //     }
    // }

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
        State = NodeState.Leader; // Transition to leader
        SendHeartbeat(); // Send heartbeat to the cluster
    }

}
