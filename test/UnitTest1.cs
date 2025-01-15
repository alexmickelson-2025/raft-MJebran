using FluentAssertions.Equivalency;
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


    



}
