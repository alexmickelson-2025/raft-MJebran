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

  // Highest committed index from the leader is included in AppendEntries RPC's
  [Fact]
  public void AppendEntriesIncludesHighestCommittedIndex()
  {
    // Arrange
    var leader = new RaftNode { State = NodeState.Leader };
    leader.SetCommitIndexForTesting(5);

    var followerMock = Substitute.For<IRaftNode>();
    leader.OtherNodes = new List<IRaftNode> { followerMock };

    // Act
    leader.SendHeartbeat();

    // Assert
    followerMock.Received().HandleAppendEntries(
        Arg.Is<AppendEntriesRPC>(rpc => rpc.CommitIndex == leader.CommitIndex)
    );
  }

  //When a follower learns that a log entry is committed, it applies the entry to its local state machine
  [Fact]
  public void FollowerAppliesCommittedEntryToStateMachine()
  {
    // Arrange
    var follower = new RaftNode { State = NodeState.Follower };

    var logEntry = new LogEntry(1, "Set key1=value1");
    follower.Log.Add(logEntry);
    follower.SetCommitIndexForTesting(0);

    bool isAppliedToStateMachine = false;
    follower.OnApplyLogEntry = (entry) =>
    {
      if (entry == logEntry)
      {
        isAppliedToStateMachine = true;
      }
    };

    // Act
    follower.ApplyCommittedEntries();

    // Assert
    Assert.True(isAppliedToStateMachine, "The committed log entry was not applied to the follower's state machine.");
  }


  //when the leader has received a majority confirmation of a log, it commits it
  [Fact]
  public void LeaderCommitsLogAfterMajorityAcknowledgment()
  {
    // Arrange
    var leader = new RaftNode { State = NodeState.Leader, CurrentTerm = 1 };
    var follower1 = Substitute.For<IRaftNode>();
    var follower2 = Substitute.For<IRaftNode>();
    var follower3 = Substitute.For<IRaftNode>();

    leader.OtherNodes = new List<IRaftNode> { follower1, follower2, follower3 };

    var logEntry = new LogEntry(1, "Set key1=value1");
    leader.Log.Add(logEntry);

    var rpc = new AppendEntriesRPC(leader.Id, leader.CurrentTerm, new List<LogEntry> { logEntry }, leader.CommitIndex);

    follower1.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = true });
    follower2.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = true });
    follower3.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = false });

    // Act
    foreach (var follower in leader.OtherNodes)
    {
      var response = follower.ProcessAppendEntries(rpc);
      if (response.Success)
      {
        leader.ReceiveAppendEntriesAck(follower.Id, logEntry);
      }
    }

    leader.UpdateCommitIndex();

    // Assert
    Assert.Equal(0, leader.CommitIndex);

  }

  //the leader commits logs by incrementing its committed log index
  [Fact]
  public void LeaderIncrementsCommitIndexWhenLogsAreCommitted()
  {
    // Arrange
    var leader = new RaftNode { State = NodeState.Leader, CurrentTerm = 1 };
    var follower1 = Substitute.For<IRaftNode>();
    var follower2 = Substitute.For<IRaftNode>();
    var follower3 = Substitute.For<IRaftNode>();

    leader.OtherNodes = new List<IRaftNode> { follower1, follower2, follower3 };

    var logEntry1 = new LogEntry(1, "Set key1=value1");
    var logEntry2 = new LogEntry(1, "Set key2=value2");
    leader.Log.Add(logEntry1);
    leader.Log.Add(logEntry2);

    var rpc = new AppendEntriesRPC(leader.Id, leader.CurrentTerm, leader.Log, leader.CommitIndex);
    follower1.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = true });
    follower2.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = true });
    follower3.ProcessAppendEntries(rpc).Returns(new AppendEntriesResponse { Success = false });

    // Act
    foreach (var follower in leader.OtherNodes)
    {
      var response = follower.ProcessAppendEntries(rpc);
      if (response.Success)
      {
        leader.ReceiveAppendEntriesAck(follower.Id, logEntry1);
        leader.ReceiveAppendEntriesAck(follower.Id, logEntry2);
      }
    }

    leader.UpdateCommitIndex();

    // Assert
    Assert.Equal(1, leader.CommitIndex);
  }

  // given a follower receives an appendentries with log(s) it will add those entries to its personal log
  [Fact]
  public void FollowerAppendsLogEntriesFromAppendEntriesRPC()
  {
    // Arrange
    var follower = new RaftNode { State = NodeState.Follower, CurrentTerm = 1 };

    var incomingLogEntries = new List<LogEntry>
    {
        new LogEntry(1, "Set key1=value1"),
        new LogEntry(1, "Set key2=value2")
    };

    var appendEntriesRpc = new AppendEntriesRPC(
        leaderId: Guid.NewGuid(),
        term: 1,
        entries: incomingLogEntries,
        commitIndex: 0
    );

    // Act
    follower.HandleAppendEntries(appendEntriesRpc);

    // Assert
    Assert.Equal(2, follower.Log.Count);
    Assert.Equal(incomingLogEntries[0].Command, follower.Log[0].Command); 
    Assert.Equal(incomingLogEntries[1].Command, follower.Log[1].Command); 
  }

}