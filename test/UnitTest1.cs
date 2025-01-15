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

   


}
