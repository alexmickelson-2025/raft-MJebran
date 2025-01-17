﻿using FluentAssertions.Equivalency;
using logic;
namespace test;

public class LeaderElectionTests
{
    // [Fact]
    // public void FollowerStaysFollowerOnHeartbeat()
    // {
    //     //Given: A follower gets a heartbeat message within the timeout.
    //     var node = new RaftNode();
    //     node.State = NodeState.Follower;
    //     // When: It processes the heartbeat.
    //     node.processHeartbeat();
    //     // Then: It stays a follower.
    //     Assert.Equal(NodeState.Follower, node.State);
    // }



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
        var node = new RaftNode { CurrentTerm = 1 };

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





}
