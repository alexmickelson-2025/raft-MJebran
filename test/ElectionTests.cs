using FluentAssertions.Equivalency;
using logic;
namespace test;

public class LeaderElectionTests
{
    // Testing according to part Two of Readme
    // Testing # 1: Node Initialization
    [Fact]
    public void TestNodeInitialization_DefaultStateIsFollower()
    {
        // Arrange
        var node = new RaftNode();
        // Act
        var state = node.State;
        // Assert
        Assert.Equal(NodeState.Follower, state);
    }

    // Testing #2 Candidate Self Voting
    [Fact]
    public void TestCandidateSelfVoting_VotesForItself()
    {
        // Arrange
        var node = new RaftNode();
        node.BecomeCandidate();

        // Act
        var voteCount = node.GetVoteCount();

        // Assert
        Assert.Equal(1, voteCount);
        Assert.True(node.HasVotedFor(node.Id));
    }

    // Testing # 3 Leader Recognition
    [Fact]
    public void TestLeaderRecognition_RemembersLeaderOnAppendEntries()
    {
        // Arrange
        var node = new RaftNode();
        var leaderId = Guid.NewGuid();
        var appendEntries = new AppendEntriesRPC(leaderId, 1, new List<LogEntry>());

        // Act
        node.HandleAppendEntries(appendEntries);

        // Assert
        Assert.Equal(leaderId, node.CurrentLeaderId);
    }

    // Testing # 4 AppendEntries Response
    [Fact]
    public void TestAppendEntriesResponse_SendsResponseOnRequest()
    {
        // Arrange
        var node = new RaftNode();
        var appendEntries = new AppendEntriesRPC(Guid.NewGuid(), 1, new List<LogEntry>());

        // Act
        var response = node.ProcessAppendEntries(appendEntries);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
    }

    // Testing # 5 Voting for Previous Term
    [Fact]
    public void TestVotingForPreviousTerm_VotesYes()
    {
        // Arrange
        var node = new RaftNode { CurrentTerm = 1 };
        var requestForVote = new RequestForVoteRPC(term: 2, candidateId: Guid.NewGuid());

        // Act
        var response = node.HandleRequestForVote(requestForVote);

        // Assert
        Assert.True(response.VoteGranted);
        Assert.Equal(2, node.CurrentTerm);
    }

    // Testing # 6 Randomized Election Timeout
    [Fact]
    public void TestRandomizedElectionTimeout_IsWithinRange()
    {
        // Arrange
        var node = new RaftNode();
        var timeouts = new List<int>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            node.ResetElectionTimer();
            timeouts.Add(node.ElectionTimeout);
        }

        // Assert
        Assert.All(timeouts, timeout => Assert.InRange(timeout, 150, 300));
        Assert.Contains(timeouts, t => t != timeouts.First());
    }

    // Testing # 7 Term Increment on Election Start
    [Fact]
    public void TestTermIncrementOnElectionStart_IncreasesTerm()
    {
        // Arrange
        var node = new RaftNode
        {
            OtherNodes = new List<IRaftNode>()
        };
        node.CurrentTerm = 1;

        // Act
        node.StartElection();

        // Assert
        Assert.Equal(2, node.CurrentTerm);
    }


    // Testing # 8 Follower Receiving Lower Term AppendEntries
    [Fact]
    public void TestFollowerReceivesLaterTermAppendEntries_BecomesFollower()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Candidate, CurrentTerm = 1 };
        var appendEntries = new AppendEntriesRPC(Guid.NewGuid(), term: 2, new List<LogEntry>());

        // Act
        node.HandleAppendEntries(appendEntries);

        // Assert
        Assert.Equal(NodeState.Follower, node.State); // Ensure state changes to Follower
        Assert.Equal(2, node.CurrentTerm); // Ensure term is updated
    }

    // Testing # 9 Election Timer Expiry
    [Fact]
    public async Task TestElectionTimerExpiry_StartsElection()
    {
        // Arrange
        var node = new RaftNode();
        node.StartElectionTimer(300);
        // Act
        await Task.Delay(310);
        node.CheckElectionTimeout();

        // Assert
        Assert.Equal(NodeState.Candidate, node.State);
    }

    // Testing # 10
    // A node becomes a candidate (BecomeCandidate).
    // Votes are received via ReceiveVote().
    // Check if the node has received the majority of votes using HasMajorityVotes(totalNodes).
    // If the majority is achieved, the candidate transitions to the leader state.

    [Fact]
    public void TestCandidateBecomesLeader_WhenMajorityVotesReceived()
    {
        // Arrange
        var node = new RaftNode();
        node.BecomeCandidate();
        var totalNodes = 3;

        // Act
        for (int i = 0; i < 2; i++)
        {
            node.ReceiveVote();
        }

        if (node.HasMajorityVotes(totalNodes))
        {
            node.State = NodeState.Leader;
        }

        // Assert
        Assert.Equal(NodeState.Leader, node.State);
    }

    // Testing # 11 Leader Heartbeat
    [Fact]
    public async Task TestLeaderSendsHeartbeatWithin50ms()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Leader };
        var heartbeatSent = false;

        // Mock method to simulate sending heartbeat
        node.OnHeartbeat = () => heartbeatSent = true;

        // Act
        node.StartHeartbeatTimer(50);
        await Task.Delay(60);

        // Assert
        Assert.True(heartbeatSent);
    }

    // Testing # 12 When a follower does get an AppendEntries message, it resets the election timer.   
    [Fact]
    public void TestFollowerResetsElectionTimerOnAppendEntries()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Follower };
        var appendEntries = new AppendEntriesRPC(Guid.NewGuid(), term: 1, new List<LogEntry>());
        var initialElectionTimeout = node.ElectionTimeout;

        // Act
        node.HandleAppendEntries(appendEntries);
        var newElectionTimeout = node.ElectionTimeout;

        // Assert
        Assert.NotEqual(initialElectionTimeout, newElectionTimeout);
        Assert.True(newElectionTimeout >= 150 && newElectionTimeout <= 300);
    }

    // Testing # 13  Given a candidate receives a majority of votes while waiting for an unresponsive node, it still becomes a leader.
    [Fact]
    public void TestCandidateBecomesLeader_WithMajorityVotesDespiteUnresponsiveNodes()
    {
        // Arrange
        var node = new RaftNode();
        node.BecomeCandidate();

        int totalNodes = 3;
        int majority = (totalNodes / 2) + 1;
        int votesReceived = 0;

        // Simulate responses
        for (int i = 0; i < majority; i++)
        {
            // Simulate receiving votes from a responsive node
            node.ReceiveVote();
            votesReceived++;
        }

        // Act
        bool hasMajority = node.HasMajorityVotes(totalNodes);
        if (hasMajority)
        {
            node.State = NodeState.Leader; // Transition to leader
        }

        // Assert
        Assert.Equal(NodeState.Leader, node.State); // Node becomes leader
        Assert.Equal(majority, votesReceived); // Majority of votes were received
    }

    // Testing # 14 A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes.
    [Fact]
    public void TestFollowerVotesForRequestInEarlierTerm()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Follower, CurrentTerm = 1, VotedFor = null };
        var candidateId = Guid.NewGuid();
        var requestForVote = new RequestForVoteRPC(term: 2, candidateId: candidateId);

        // Act
        var response = node.HandleRequestForVote(requestForVote);

        // Assert
        Assert.True(response.VoteGranted); // Ensure the vote is granted
        Assert.Equal(2, node.CurrentTerm); // Ensure the term is updated
        Assert.Equal(candidateId, node.VotedFor); // Ensure the candidate is recorded as voted for
    }

    //Testing # 15  Given a candidate, when it receives an AppendEntries message from a node with an equal term, the candidate loses and becomes a follower.
    [Fact]
    public void TestCandidateBecomesFollowerOnAppendEntriesWithEqualTerm()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Candidate, CurrentTerm = 2 };
        var leaderId = Guid.NewGuid();
        var appendEntries = new AppendEntriesRPC(leaderId, term: 2, new List<LogEntry>());

        // Act
        node.HandleAppendEntries(appendEntries);

        // Assert
        Assert.Equal(NodeState.Follower, node.State);
        Assert.Equal(2, node.CurrentTerm);
        Assert.Equal(leaderId, node.CurrentLeaderId);
    }

    // Testing # 16 If a node receives a second request for vote for the same term, it should respond no.
    [Fact]
    public void TestRejectsDuplicateVoteRequestForSameTerm()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Follower, CurrentTerm = 3, VotedFor = Guid.NewGuid() };
        var newCandidateId = Guid.NewGuid(); // A different candidate than the one already voted for
        var requestForVote = new RequestForVoteRPC(term: 3, candidateId: newCandidateId);

        // Act
        var response = node.HandleRequestForVote(requestForVote);

        // Assert
        Assert.False(response.VoteGranted);
        Assert.Equal(3, node.CurrentTerm);
        Assert.NotEqual(newCandidateId, node.VotedFor);
    }

    // Testing # 17  If a node receives a second request for vote for a future term, it should vote for that node.
    [Fact]
    public void TestVotesForFutureTermRequest()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Follower, CurrentTerm = 2, VotedFor = null };
        var futureTerm = 3; // A term greater than the current term
        var candidateId = Guid.NewGuid();
        var requestForVote = new RequestForVoteRPC(term: futureTerm, candidateId: candidateId);

        // Act
        var response = node.HandleRequestForVote(requestForVote);

        // Assert
        Assert.True(response.VoteGranted);
        Assert.Equal(futureTerm, node.CurrentTerm);
        Assert.Equal(candidateId, node.VotedFor);
    }

    // Testing # 18 Given a candidate, when an election timer expires inside of an election, a new election is started.
    [Fact]
    public async Task TestElectionRestartOnTimerExpiry()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Candidate, CurrentTerm = 3 };
        node.StartElectionTimer(200); // Set an election timer of 200ms

        // Act
        await Task.Delay(210);
        node.CheckElectionTimeout();

        // Assert
        Assert.Equal(NodeState.Candidate, node.State);
        Assert.Equal(4, node.CurrentTerm);
        Assert.True(node.HasVotedFor(node.Id));
    }

    // Testing # 19  When a follower node receives an AppendEntries request, it sends a response.
    [Fact]
    public void TestFollowerSendsResponseToAppendEntries()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Follower, CurrentTerm = 3 };
        var leaderId = Guid.NewGuid();
        var appendEntries = new AppendEntriesRPC(leaderId, term: 3, new List<LogEntry>());

        // Act
        var response = node.ProcessAppendEntries(appendEntries);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(3, node.CurrentTerm);
        Assert.Equal(leaderId, node.CurrentLeaderId);
    }

    // Testing # 20  Given a candidate receives an AppendEntries from a previous term, then it rejects the request.
    [Fact]
    public void TestCandidateRejectsAppendEntriesFromPreviousTerm()
    {
        // Arrange
        var node = new RaftNode { State = NodeState.Candidate, CurrentTerm = 3 };
        var leaderId = Guid.NewGuid();
        var appendEntries = new AppendEntriesRPC(leaderId, term: 2, new List<LogEntry>()); // Term is lower than current term

        // Act
        var response = node.ProcessAppendEntries(appendEntries);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal(3, node.CurrentTerm);
        Assert.Null(node.CurrentLeaderId);
    }

    // Testing 21  When a candidate wins an election, it immediately sends a heartbeat.
    // [Fact]
    // public void TestLeaderSendsHeartbeatAfterWinningElection()
    // {
    //     // Arrange
    //     var node = new RaftNode { State = NodeState.Candidate, CurrentTerm = 3 };
    //     var mockCluster = new MockCluster();
    //     node.SetCluster(mockCluster);

    //     // Act
    //     node.BecomeLeader(); 

    //     // Assert
    //     Assert.Equal(NodeState.Leader, node.State); 
    //     Assert.Equal(3, node.CurrentTerm); 
    //     Assert.True(mockCluster.HeartbeatsSent); 
    // }

    // -------------------------------------------5.3 & 5.4.2-------------------------------------------
    // Testing # 22 section 5.3 & 5.4.2 Follower becomes candidate after timeout
    [Fact]
    public void FollowerBecomesCandidateAfterTimeout()
    {
        // Arrange
        var node = new RaftNode();
        node.CurrentTerm = 1;
        node.State = NodeState.Follower;
        node.LastHeartbeat = DateTime.UtcNow.AddMilliseconds(-node.ElectionTimeout - 1);

        // Act
        node.CheckElectionTimeout();

        // Assert
        Assert.Equal(NodeState.Candidate, node.State);
        Assert.Equal(2, node.CurrentTerm); 
        Assert.Equal(node.Id, node.VotedFor);
    }



}
