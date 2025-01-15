public class LogEntry
{
    public int Term { get; set; }
    public string Command { get; set; }

    public LogEntry(int term, string command)
    {
        Term = term;
        Command = command;
    }
}