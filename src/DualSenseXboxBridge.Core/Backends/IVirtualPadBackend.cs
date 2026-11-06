using DualSenseXboxBridge.Core.Mapping;

namespace DualSenseXboxBridge.Core.Backends;

/// <summary>
/// A virtual Xbox 360 pad. Implementations are interchangeable so the DualSense reader and
/// the mapping layer never depend on a specific driver.
/// </summary>
public interface IVirtualPadBackend : IAsyncDisposable
{
    string Name { get; }

    VirtualPadBackendKind Kind { get; }

    bool IsConnected { get; }

    string StatusMessage { get; }

    event EventHandler<BackendStatusEventArgs>? StatusChanged;

    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>Pushes one state. Called from the input pump; must not block for long.</summary>
    void Submit(in XboxPadState state);

    Task DisconnectAsync(CancellationToken cancellationToken);
}
