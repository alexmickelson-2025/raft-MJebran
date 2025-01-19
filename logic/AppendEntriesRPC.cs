public class AppendEntriesRPC
{
    public Guid LeaderId { get; private set; }
    public int Term { get; private set; }
    public List<LogEntry> Entries { get; private set; }

    public AppendEntriesRPC(Guid leaderId, int term, List<LogEntry> entries)
    {
        LeaderId = leaderId;
        Term = term;
        Entries = entries ?? new List<LogEntry>();
    }
}

public class AppendEntriesResponse
{
    public bool Success { get; set; }
}