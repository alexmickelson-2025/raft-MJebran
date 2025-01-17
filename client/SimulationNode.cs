using client;
using logic;

public class SimulationNode : IRaftNode
{
    public RaftNode InnerNode;
    public SimulationNode(RaftNode node)
    {
        this.InnerNode = node;
    }

    public Guid Id => InnerNode.Id;

    public NodeState State { get => ((IRaftNode)InnerNode).State; set => ((IRaftNode)InnerNode).State = value; }

    public Guid? CurrentLeaderId => ((IRaftNode)InnerNode).CurrentLeaderId;

    public int CurrentTerm { get => ((IRaftNode)InnerNode).CurrentTerm; set => ((IRaftNode)InnerNode).CurrentTerm = value; }
    public Guid? VotedFor { get => ((IRaftNode)InnerNode).VotedFor; set => ((IRaftNode)InnerNode).VotedFor = value; }
    public List<IRaftNode> OtherNodes { get => ((IRaftNode)InnerNode).OtherNodes; set => ((IRaftNode)InnerNode).OtherNodes = value; }

    public void BecomeCandidate()
    {
        ((IRaftNode)InnerNode).BecomeCandidate();
    }

    public void BecomeLeader()
    {
        ((IRaftNode)InnerNode).BecomeLeader();
    }

    public void CheckElectionTimeout()
    {
        ((IRaftNode)InnerNode).CheckElectionTimeout();
    }

    public void HandleAppendEntries(AppendEntriesRPC appendEntries)
    {
        ((IRaftNode)InnerNode).HandleAppendEntries(appendEntries);
    }

    public RequestForVoteResponse HandleRequestForVote(RequestForVoteRPC rpc)
    {
        return ((IRaftNode)InnerNode).HandleRequestForVote(rpc);
    }

    public bool HasMajorityVotes(int totalNodes)
    {
        return ((IRaftNode)InnerNode).HasMajorityVotes(totalNodes);
    }

    public AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPC rpc)
    {
        return ((IRaftNode)InnerNode).ProcessAppendEntries(rpc);
    }

    public void ReceiveVote()
    {
        ((IRaftNode)InnerNode).ReceiveVote();
    }

    public void ResetElectionTimer()
    {
        ((IRaftNode)InnerNode).ResetElectionTimer();
    }

    public void StartElection()
    {
        ((IRaftNode)InnerNode).StartElection();
    }

    public void StartElectionTimer(int timeoutMs)
    {
        ((IRaftNode)InnerNode).StartElectionTimer(timeoutMs);
    }

    public void StartHeartbeatTimer(int intervalMs)
    {
        ((IRaftNode)InnerNode).StartHeartbeatTimer(intervalMs);
    }

    public void StopHeartbeatTimer()
    {
        ((IRaftNode)InnerNode).StopHeartbeatTimer();
    }
}