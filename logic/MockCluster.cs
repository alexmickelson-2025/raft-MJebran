namespace test;
public class MockCluster
{
    public bool HeartbeatsSent { get; private set; } = false;

    public void SendHeartbeat(AppendEntriesRPC heartbeat)
    {
       // Simulate sending heartbeats
        HeartbeatsSent = true;
    }
}
