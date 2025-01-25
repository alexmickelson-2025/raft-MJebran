using logic;
using NSubstitute;
namespace test;

public class LogTests
{

  //  when a leader receives a client command the leader sends the log entry in the next appendentries RPC to all nodes
  [Fact]
  public void LeaderSendsLogEntryInNextAppendEntriesRPC()
  {
    // Arrange
    var follower1 = Substitute.For<IRaftNode>();
    var follower2 = Substitute.For<IRaftNode>();

    var leader = new RaftNode
    {
      State = NodeState.Leader,
      OtherNodes = new List<IRaftNode> { follower1, follower2 }
    };

    var command = new ClientCommandData(ClientCommandType.Set, "key1", "value1", (_, _) => { });

    // Act
    leader.SendCommand(command);

    // Assert
    follower1.Received(1).HandleAppendEntries(Arg.Is<AppendEntriesRPC>(rpc =>
        rpc.Entries.Count == 1 && rpc.Entries[0].Command == "Set key1=value1"));

    follower2.Received(1).HandleAppendEntries(Arg.Is<AppendEntriesRPC>(rpc =>
        rpc.Entries.Count == 1 && rpc.Entries[0].Command == "Set key1=value1"));
  }

  // when a leader receives a command from the client, it is appended to its log
  [Fact]
  public void LeaderAppendsCommandToItsLog()
  {
    // Arrange
    var leaderNode = new RaftNode { State = NodeState.Leader };
    leaderNode.OtherNodes = new List<IRaftNode>();

    var command = new ClientCommandData(
        ClientCommandType.Set, 
        "key1",                
        "value1",             
        Substitute.For<Action<bool, Guid?>>() 
    );

    // Act
    leaderNode.SendCommand(command);

    // Assert
    Assert.Single(leaderNode.Log); 
    var logEntry = leaderNode.Log[0];
    Assert.Equal("Set key1=value1", logEntry.Command); 
    Assert.Equal(leaderNode.CurrentTerm, logEntry.Term);
  }






}