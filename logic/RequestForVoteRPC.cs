namespace logic;
public record RequestForVoteRPCDTO
{
    public int Term { get; private set; }
    public Guid CandidateId { get; private set; }

    public RequestForVoteRPCDTO(int term, Guid candidateId)
    {
        Term = term;
        CandidateId = candidateId;
    }
}

public class RequestForVoteResponse
{
    public bool VoteGranted { get; set; }
}