namespace logic;

public enum NodeState
{
    Follower,
    Candidate,
    Leader
}

public class RaftNode
{
    public Guid Id { get; private set; }
    public NodeState State { get; set; }
    public Guid? CurrentLeaderId { get; private set; }
    public int CurrentTerm { get; set; }
    public Guid? VotedFor { get; private set; }
    public int ElectionTimeout { get; private set; }
    private DateTime LastHeartbeat;

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
    if (appendEntries.Term >= CurrentTerm)
    {
        CurrentTerm = appendEntries.Term; 
        State = NodeState.Follower; 
        CurrentLeaderId = appendEntries.LeaderId; 
    }
    LastHeartbeat = DateTime.UtcNow; 
}


    // Test #4: AppendEntries Response
    public AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPC rpc)
    {
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

        if (VotedFor == null || VotedFor == rpc.CandidateId)
        {
            VotedFor = rpc.CandidateId;
            return new RequestForVoteResponse { VoteGranted = true };
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

    // Test #8: Follower Receiving Later Term AppendEntries
    public void ProcessAppendEntriesWithLaterTerm(AppendEntriesRPC rpc)
    {
        if (rpc.Term > CurrentTerm)
        {
            CurrentTerm = rpc.Term;
            State = NodeState.Follower;
            CurrentLeaderId = rpc.LeaderId;
        }
    }

    // Test #9: Duplicate Vote Request Handling
    public bool IsDuplicateVote(RequestForVoteRPC rpc)
    {
        return rpc.Term == CurrentTerm && VotedFor != null && VotedFor != rpc.CandidateId;
    }

    // Test #10: Election Timer Expiry
    public void StartElectionTimer(int timeoutMs)
    {
        LastHeartbeat = DateTime.UtcNow.AddMilliseconds(-timeoutMs);
    }

    public void CheckElectionTimeout()
    {
        if (State == NodeState.Follower && (DateTime.UtcNow - LastHeartbeat).TotalMilliseconds > ElectionTimeout)
        {
            StartElection();
        }
    }
}
