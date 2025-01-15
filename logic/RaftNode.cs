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
        // ResetElectionTimer();
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


    
}
