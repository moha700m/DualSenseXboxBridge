namespace DualSenseXboxBridge;

public interface IVirtualPadBackend : IDisposable
{
    string Name { get; }
    bool IsConnected { get; }
    void Connect();
    void Disconnect();
    void Submit(XboxPadState state);
}

public sealed record XboxPadState(
    ushort Buttons,
    byte LeftTrigger,
    byte RightTrigger,
    short LeftX,
    short LeftY,
    short RightX,
    short RightY);
