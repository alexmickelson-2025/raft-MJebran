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
    public int ElectionTimeout { get; private set; }

    public RaftNode()
    {
        Id = Guid.NewGuid();
        State = NodeState.Follower; 
        ResetElectionTimer();
    }

    public NodeState GetState() => State;

    public void ResetElectionTimer()
    {
        var random = new Random();
        ElectionTimeout = random.Next(150, 301); 
    }
}
