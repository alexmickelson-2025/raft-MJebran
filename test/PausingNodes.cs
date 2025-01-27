using System.Runtime.CompilerServices;
using logic;
using Xunit.Sdk;
using NSubstitute;


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
        follower1.DidNotReceive().ProcessAppendEntries(Arg.Any<AppendEntriesRPC>());
        follower2.DidNotReceive().ProcessAppendEntries(Arg.Any<AppendEntriesRPC>());

    }

    // Testing #2 IN_CLASS When no node is a leader with an election loop, then they get paused, other nodes do not get heartbeat for 400 ms, then they get un-paused and heartbeats resume
    







    // Testing #3 IN_CLASS When a follower gets paused, it does not time out to become a candidate


}