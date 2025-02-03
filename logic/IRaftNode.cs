namespace logic
{
    public interface IRaftNode
    {
        public Guid Id { get; set;}
        NodeState State { get; set; }
        Guid? CurrentLeaderId { get; set; }
        int CurrentTerm { get; set; }
        List<IRaftNode> OtherNodes { get; set; }
        Guid? VotedFor { get; set; }
        void BecomeCandidate();
        void BecomeLeader();
        void HandleAppendEntries(AppendEntriesRPCDTO appendEntries);
        AppendEntriesResponse ProcessAppendEntries(AppendEntriesRPCDTO rpc);
        RequestForVoteResponse HandleRequestForVote(RequestForVoteRPCDTO rpc);
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
