namespace logic;

public record AppendEntriesRPCDTO
{
    public Guid LeaderId { get; private set; }
    public int Term { get; private set; }
    public List<LogEntry> Entries { get; private set; }
    public int CommitIndex { get; set; }

    public AppendEntriesRPCDTO(Guid leaderId, int term, List<LogEntry> entries, int commitIndex)
    {
        LeaderId = leaderId;
        Term = term;
        Entries = entries;
        CommitIndex = commitIndex;
    }

}

public class AppendEntriesResponse
{
    public bool Success { get; set; }
}