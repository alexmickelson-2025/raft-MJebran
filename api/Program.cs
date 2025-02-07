using System.Text.Json;
// using OpenTelemetry.Logs;
// using OpenTelemetry.Resources;
// using RaftLogic;
using logic;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");


var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

builder.Services.AddLogging();
var serviceName = "Node" + nodeId;
// builder.Logging.AddOpenTelemetry(options =>
// {
//   options
//     .SetResourceBuilder(
//         ResourceBuilder
//           .CreateDefault()
//           .AddService(serviceName)
//     )
//     .AddOtlpExporter(options =>
//     {
//       options.Endpoint = new Uri("http://dashboard:18889");
//     });
// });
var app = builder.Build();


IRaftNode[] otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcOtherNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
  .ToArray();


var node = new RaftNode{Id = IntToGuid(int.Parse(nodeId)), OtherNodes = new List<IRaftNode>(otherNodes)};

// RaftNode.NodeIntervalScalar = double.Parse(nodeIntervalScalarRaw);

node.RunElectionLoop();

app.MapGet("/health", () => "healthy");

app.MapGet("/nodeData", () =>
{
  return new NodeData
  {
    Id = node.Id,
    State = node.State,
    ElectionTimeout = node.ElectionTimeout,
    CurrentTerm = node.CurrentTerm,
    CurrentLeaderId = node.CurrentLeaderId,
    CommittedEntryIndex = node.CommitIndex,
    Log =node.Log,
    // NodeIntervalScalar: RaftNode.NodeIntervalScalar
  }
    
  ;
});

app.MapPost("/request/appendEntries", async (AppendEntriesRPCDTO request) =>
{
  node.HandleAppendEntries(request);
});

app.MapPost("/request/vote", async (RequestForVoteRPCDTO request) =>
{
  node.HandleRequestForVote(request);
});

app.MapPost("/response/appendEntries", async (AppendEntriesRPCDTO response) =>
{
  node.ProcessAppendEntries(response);
});

app.MapPost("/response/vote", async (RequestForVoteRPCDTO response) =>
{
  node.HandleRequestForVote(response);
});

// app.MapPost("/request/command", async (ClientCommandData data) =>
// {
//   node.SendCommand(data);
// });

app.MapPost("/request/command", async (HttpContext context) =>
{
    try
    {
        var commandData = await context.Request.ReadFromJsonAsync<ClientCommandData>();
        if (commandData == null)
        {
            throw new ArgumentNullException(nameof(commandData), "Command data cannot be null.");
        }

        var safeCommandData = commandData with
        {
            RespondToClient = commandData.RespondToClient ?? ((success, id) => Console.WriteLine($"Command executed: {success}, Leader: {id}"))
        };

        node.SendCommand(safeCommandData);
        context.Response.StatusCode = StatusCodes.Status200OK;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing command: {ex.Message}");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
});

app.MapPost("/startSimulation", async (HttpContext context) =>
{
    try
    {
        Console.WriteLine("Starting Raft Simulation...");

        node.RunElectionLoop(); 

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("Simulation started.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error starting simulation: {ex.Message}");
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