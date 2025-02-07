

namespace logic;

public class HttpRpcOtherNode : IRaftNode
{
  public int Id { get; }
  public string Url { get; }

  Guid IRaftNode.Id { get; set; }

  NodeState IRaftNode.State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  Guid? IRaftNode.CurrentLeaderId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  int IRaftNode.CurrentTerm { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  List<IRaftNode> IRaftNode.OtherNodes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  Guid? IRaftNode.VotedFor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

  private HttpClient client = new();

  public HttpRpcOtherNode(int id, string url)
  {
    Id = id;
    Url = url;
  }

  public async Task RequestAppendEntries(AppendEntriesRPCDTO request)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task RequestVote(RequestForVoteRPCDTO request)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/vote", request);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task RespondAppendEntries(AppendEntriesResponse response)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task ResponseVote(RequestForVoteResponse response)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/vote", response);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task SendCommand(ClientCommandData data)
  {
    await client.PostAsJsonAsync(Url + "/request/command", data);
  }

  void IRaftNode.BecomeCandidate()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.BecomeLeader()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.HandleAppendEntries(AppendEntriesRPCDTO appendEntries)
  {
    try
    {
      client.PostAsJsonAsync(Url + "/request/appendEntries", appendEntries);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  AppendEntriesResponse IRaftNode.ProcessAppendEntries(AppendEntriesRPCDTO rpc)
  {
    try
    {
      client.PostAsJsonAsync(Url + "/response/appendEntries", rpc);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
    return new AppendEntriesResponse();
  }

  RequestForVoteResponse IRaftNode.HandleRequestForVote(RequestForVoteRPCDTO rpc)
  {
    try
    {
      client.PostAsJsonAsync(Url + "/request/vote", rpc);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
    return new RequestForVoteResponse();
  }

  void IRaftNode.ResetElectionTimer()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.StartElection()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.StartHeartbeatTimer(int intervalMs)
  {
    throw new NotImplementedException();
  }

  void IRaftNode.StopHeartbeatTimer()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.StartElectionTimer(int timeoutMs)
  {
    throw new NotImplementedException();
  }

  void IRaftNode.CheckElectionTimeout()
  {
    throw new NotImplementedException();
  }

  void IRaftNode.ReceiveVote()
  {
    throw new NotImplementedException();
  }

  bool IRaftNode.HasMajorityVotes(int totalNodes)
  {
    throw new NotImplementedException();
  }
}