using logic;
public record NodeData
{
  public Guid Id { get; set; }
  public NodeState State { get; set; }
  public Guid? CurrentLeaderId { get; set; }
  public int CurrentTerm { get; set; }
  public List<IRaftNode> OtherNodes { get; set; }
  public Guid? VotedFor { get; set; }
  public int ElectionTimeout { get; set; }
  public int CommittedEntryIndex { get; set; }
  public List<LogEntry> Log { get; set; }
  public double NodeIntervalScalar { get; set; }

}