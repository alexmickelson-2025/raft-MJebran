using logic;
using NSubstitute;
namespace test;


public class PausingNodes
{
    // Testing #1 IN_CLASSWhen node is a leader with an election loop, they get paused, other nodes do not get heartbeat for 400 ms
    [Fact]
    public async Task LeaderPause_StopsHeartbeat_FollowersTriggerElection()
    {
        // Arrange
        var leader = new RaftNode { State = NodeState.Leader, CurrentTerm = 1 };
        var follower1 = Substitute.For<IRaftNode>();
        var follower2 = Substitute.For<IRaftNode>();

        leader.OtherNodes = new List<IRaftNode> { follower1, follower2 };
        leader.StartHeartbeatTimer(100);
        leader.StopHeartbeatTimer();
        follower1.ClearReceivedCalls();
        follower2.ClearReceivedCalls();
        await Task.Delay(400);

        // Assert: 
        follower1.DidNotReceive().ProcessAppendEntries(Arg.Any<AppendEntriesRPCDTO>());
        follower2.DidNotReceive().ProcessAppendEntries(Arg.Any<AppendEntriesRPCDTO>());

    }

    // Testing #3 IN_CLASS When a follower gets paused, it does not time out to become a candidate
    [Fact]
    public async Task FollowerDoesNotTimeoutToBecomeCandidateWhenPaused()
    {
        // Arrange
        var follower = new RaftNode { State = NodeState.Follower, CurrentTerm = 1 };
        follower.StartElectionTimer(300);
        follower.PauseElectionLoop();

        // Act
        await Task.Delay(400);

        // Assert
        Assert.Equal(NodeState.Follower, follower.State);
    }
}