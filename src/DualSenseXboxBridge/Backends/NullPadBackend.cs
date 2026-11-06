using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Mapping;

namespace DualSenseXboxBridge.Backends;

/// <summary>No-op output used by the live tester when no virtual pad driver is available.</summary>
public sealed class NullPadBackend : IVirtualPadBackend
{
    private XboxPadState _last;

    public string Name => "وضع الاختبار (بدون جهاز افتراضي)";

    public VirtualPadBackendKind Kind => VirtualPadBackendKind.Null;

    public bool IsConnected { get; private set; }

    public string StatusMessage { get; private set; } = "غير متصل";

    public event EventHandler<BackendStatusEventArgs>? StatusChanged;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = true;
        StatusMessage = "الاختبار يعمل: تتم قراءة اليد دون إنشاء جهاز افتراضي.";
        StatusChanged?.Invoke(this, new BackendStatusEventArgs(BackendConnectionState.Connected, StatusMessage));
        return Task.CompletedTask;
    }

    public void Submit(in XboxPadState state) => _last = state;

    public XboxPadState LastState => _last;

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = false;
        StatusMessage = "غير متصل";
        StatusChanged?.Invoke(this, new BackendStatusEventArgs(BackendConnectionState.Disconnected, StatusMessage));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }
}
