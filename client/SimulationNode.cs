using client;
using logic;
public class SimulationNode : IRaftNode
{
    public required RaftNode InnerNode;
    public SimulationNode(RaftNode node)
    {
        this.InnerNode = node;
    }

    public Guid Id => InnerNode.Id;


}