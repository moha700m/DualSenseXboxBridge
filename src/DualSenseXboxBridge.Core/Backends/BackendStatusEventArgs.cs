namespace DualSenseXboxBridge.Core.Backends;

public sealed class BackendStatusEventArgs : EventArgs
{
    public BackendStatusEventArgs(BackendConnectionState state, string message, Exception? error = null)
    {
        State = state;
        Message = message;
        Error = error;
    }

    public BackendConnectionState State { get; }

    public string Message { get; }

    public Exception? Error { get; }
}
