using logic;


public class SimulationNode : IRaftNode
{
    public RaftNode InnerNode { get; private set; }
    public string Message { get; set; } = "";
    public static int NetworkRequestDelay { get; set; } = 1000;
    public static int NetworkResponseDelay { get; set; } = 0;
    private bool simulationRunning = false;
    public string ResponseMessage { get; set; }

    // Add properties for UI command input
    public string CommandKey { get; set; } = string.Empty;
    public string CommandValue { get; set; } = string.Empty;
    public string CommandResponse { get; set; } = string.Empty;

    public SimulationNode(RaftNode node)
    {
        this.InnerNode = node;
        this.ResponseMessage = string.Empty;
    }

    public Guid Id => InnerNode.Id;

    public NodeState State
    {
        get => ((IRaftNode)InnerNode).State;
        set => ((IRaftNode)InnerNode).State = value;
    }

    public Guid? CurrentLeaderId
    {
        get => ((IRaftNode)InnerNode).CurrentLeaderId;
        set => ((IRaftNode)InnerNode).CurrentLeaderId = value;
    }

    public int CurrentTerm
    {
        get => ((IRaftNode)InnerNode).CurrentTerm;
        set => ((IRaftNode)InnerNode).CurrentTerm = value;
    }

    public Guid? VotedFor
    {
        get => ((IRaftNode)InnerNode).VotedFor;
        set => ((IRaftNode)InnerNode).VotedFor = value;
    }

    public List<IRaftNode> OtherNodes
    {
        get => ((IRaftNode)InnerNode).OtherNodes;
        set => ((IRaftNode)InnerNode).OtherNodes = value;
    }

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

    public async Task RequestVote(RequestForVoteRPC request)
    {
        if (!simulationRunning) return;

        await Task.Delay(NetworkRequestDelay);
        Message = $"Received vote request from {request.CandidateId} for term {request.Term}";
        InnerNode.HandleRequestForVote(request);
    }

    public async Task RespondAppendEntries(AppendEntriesRPC request)
    {
        if (!simulationRunning) return;

        await Task.Delay(NetworkResponseDelay);
        Message = $"Received AppendEntries from {request.LeaderId} for term {request.Term}";
        InnerNode.HandleAppendEntries(request);
    }

    public void StartSimulationLoop()
    {
        InnerNode.CancellationTokenSource = new CancellationTokenSource();
        InnerNode.RunElectionLoop();
        simulationRunning = true;
    }

    public void StopSimulationLoop()
    {
        InnerNode.CancellationTokenSource?.Cancel();

        if (State == NodeState.Leader)
        {
            State = NodeState.Candidate;
            Message = "Leader stopped, transitioning to Follower.";
        }
        else
        {
            Message = "Node stopped.";
        }

        simulationRunning = false;
    }

    public async Task SendCommand(ClientCommandData command)
    {
        if (!simulationRunning) return;

        Message = $"Processing command: {command.Key}={command.Value}";
        await Task.Run(() => InnerNode.SendCommand(command));
    }
}