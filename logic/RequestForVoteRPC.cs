public class RequestForVoteRPC
{
    public int Term { get; private set; }
    public Guid CandidateId { get; private set; }

    public RequestForVoteRPC(int term, Guid candidateId)
    {
        Term = term;
        CandidateId = candidateId;
    }
}

public class RequestForVoteResponse
{
    public bool VoteGranted { get; set; }
}
