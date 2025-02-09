using System.Text.Json;
using logic;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var nodeId = int.Parse(Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set"));
var shouldBeLeader = Environment.GetEnvironmentVariable("BE_LEADER") == "true";
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");

builder.Services.AddLogging();
var app = builder.Build();

IRaftNode[] otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcOtherNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
  .ToArray();

var node = new RaftNode { Id = IntToGuid(nodeId), OtherNodes = new List<IRaftNode>(otherNodes) };

if (nodeId == 1)  
{
    node.BecomeLeader();
}
else
{
    node.BecomeFollower();  
}

node.RunElectionLoop();

app.MapGet("/health", () => "healthy");

app.MapGet("/nodeData", () => new NodeData
{
    Id = node.Id,
    State = node.State,
    ElectionTimeout = node.ElectionTimeout,
    CurrentTerm = node.CurrentTerm,
    CurrentLeaderId = node.CurrentLeaderId,
    CommittedEntryIndex = node.CommitIndex,
    Log = node.Log
});

app.MapPost("/request/command", async (HttpContext context) =>
{
    try
    {
        var commandData = await context.Request.ReadFromJsonAsync<ClientCommandData>();
        if (commandData == null)
        {
            throw new ArgumentNullException(nameof(commandData), "Command data cannot be null.");
        }

        node.SendCommand(commandData);
        context.Response.StatusCode = StatusCodes.Status200OK;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing command: {ex.Message}");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
});

app.Run();

static Guid IntToGuid(int value)
{
    byte[] bytes = new byte[16];
    BitConverter.GetBytes(value).CopyTo(bytes, 0);
    return new Guid(bytes);
}
