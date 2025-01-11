namespace logic;

public enum NodeState
{
    Follower,
    Candidate,
    Leader
}
public class RaftNode
{
    public NodeState State { get; set; }
    public void processHeartbeat()
    {
        if (State == NodeState.Follower)
        {
        }
    }
}
