using logic;

namespace test;

public class MockCluster
{
    public bool HeartbeatsSent { get; private set; } = false;

    public void SendHeartbeat(AppendEntriesRPCDTO heartbeat)
    {
        // Simulate sending heartbeats
        HeartbeatsSent = true;
    }
}
