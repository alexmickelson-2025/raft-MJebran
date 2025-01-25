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

  // when a node is new, its log is empty
  [Fact]
  public void NewNodeLogIsEmpty()
  {
    // Arrange
    var newNode = new RaftNode();

    // Act
    var logEntries = newNode.Log;

    // Assert
    Assert.NotNull(logEntries);
    Assert.Empty(logEntries);
  }

  // when a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log
  [Fact]
  public void LeaderInitializesNextIndexForEachFollower()
  {
    // Arrange
    var leader = new RaftNode();
    leader.Log.Add(new LogEntry(1, "Set x=1"));
    leader.Log.Add(new LogEntry(1, "Set y=2"));

    var follower1 = new RaftNode();
    var follower2 = new RaftNode();

    leader.OtherNodes = new List<IRaftNode> { follower1, follower2 };

    // Act
    leader.BecomeLeader();

    // Assert
    Assert.Equal(3, leader.GetNextIndexForFollower(follower1.Id));
    Assert.Equal(3, leader.GetNextIndexForFollower(follower2.Id));
  }

  //leaders maintain an "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower
  [Fact]
  public void LeaderMaintainsNextIndexForEachFollower()
  {
    // Arrange
    var leader = new RaftNode();
    leader.Log.Add(new LogEntry(1, "Set x=1"));
    leader.Log.Add(new LogEntry(1, "Set y=2"));

    var follower1 = new RaftNode();
    var follower2 = new RaftNode();

    leader.OtherNodes = new List<IRaftNode> { follower1, follower2 };

    // Act
    leader.BecomeLeader();

    // Assert
    Assert.True(leader.HasNextIndexForFollower(follower1.Id));
    Assert.True(leader.HasNextIndexForFollower(follower2.Id));
    Assert.Equal(3, leader.GetNextIndexForFollower(follower1.Id));
    Assert.Equal(3, leader.GetNextIndexForFollower(follower2.Id));
  }






}