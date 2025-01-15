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
    public Guid? VotedFor { get; private set; }
    public int CurrentTerm { get; set; }
    public int ElectionTimeout { get; private set; }
    private DateTime LastHeartbeat;

    public RaftNode()
    {
        Id = Guid.NewGuid();
        State = NodeState.Follower; 
        VotedFor = null; 
        ResetElectionTimer();
    }

    public void BecomeCandidate()
    {
        State = NodeState.Candidate; 
        CurrentTerm++; 
        VotedFor = Id; 
    }

    public int GetVoteCount() => VotedFor == Id ? 1 : 0;

    public bool HasVotedFor(Guid candidateId) => VotedFor == candidateId;

    public void ResetElectionTimer()
    {
        var random = new Random();
        ElectionTimeout = random.Next(150, 301); // Random value between 150ms and 300ms
    }
}
