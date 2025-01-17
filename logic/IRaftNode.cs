namespace logic
{
    public interface IRaftNode
    {
        Guid Id { get; }
        NodeState State { get; set; }
        Guid? CurrentLeaderId { get; }
        int CurrentTerm { get; set; }
        List<IRaftNode> OtherNodes {get; set;}
        Guid? VotedFor { get; set; }
        void BecomeCandidate();
        void BecomeLeader();
        void HandleAppendEntries(AppendEntriesRPC appendEntries);
        AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPC rpc);
        RequestForVoteResponse HandleRequestForVote(RequestForVoteRPC rpc);
        void ResetElectionTimer();
        void StartElection();
        void StartHeartbeatTimer(int intervalMs);
        void StopHeartbeatTimer();
        void StartElectionTimer(int timeoutMs);
        void CheckElectionTimeout();
        // void SetCluster(MockCluster cluster);
        void ReceiveVote();
        bool HasMajorityVotes(int totalNodes);

    }
}
