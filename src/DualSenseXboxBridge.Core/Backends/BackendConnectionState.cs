namespace DualSenseXboxBridge.Core.Backends;

public enum BackendConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Faulted = 3
}
