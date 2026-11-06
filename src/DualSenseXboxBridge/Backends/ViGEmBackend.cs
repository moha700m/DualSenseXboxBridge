using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.Mapping;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace DualSenseXboxBridge.Backends;

/// <summary>
/// Optional legacy backend for machines that already run the signed ViGEmBus driver.
/// ViGEmBus is retired upstream, so it is never installed by this app and VIIPER is preferred.
/// </summary>
public sealed class ViGEmBackend : IVirtualPadBackend
{
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;

    public string Name => "ViGEmBus (legacy)";

    public VirtualPadBackendKind Kind => VirtualPadBackendKind.ViGEm;

    public bool IsConnected { get; private set; }

    public string StatusMessage { get; private set; } = "غير متصل";

    public event EventHandler<BackendStatusEventArgs>? StatusChanged;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _client = new ViGEmClient();
            _controller = _client.CreateXbox360Controller();
            _controller.Connect();
            IsConnected = true;
            SetStatus(BackendConnectionState.Connected, "ViGEmBus متصل (driver متقاعد لكنه موقّع).");
        }
        catch (Exception ex)
        {
            SetStatus(BackendConnectionState.Faulted, "ViGEmBus غير مثبت أو غير متاح: " + ex.Message, ex);
            throw;
        }

        return Task.CompletedTask;
    }

    public void Submit(in XboxPadState state)
    {
        var controller = _controller;
        if (controller is null)
        {
            return;
        }

        try
        {
            controller.SetButtonsFull((ushort)state.Buttons);
            controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.LeftTrigger);
            controller.SetSliderValue(Xbox360Slider.RightTrigger, state.RightTrigger);
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.LeftStickX);
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.LeftStickY);
            controller.SetAxisValue(Xbox360Axis.RightThumbX, state.RightStickX);
            controller.SetAxisValue(Xbox360Axis.RightThumbY, state.RightStickY);
            controller.SubmitReport();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            SetStatus(BackendConnectionState.Faulted, "فشل إرسال التقرير إلى ViGEm: " + ex.Message, ex);
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _controller?.Disconnect();
        }
        catch (Exception ex)
        {
            AppLog.Warn("ViGEm disconnect failed: " + ex.Message);
        }
        finally
        {
            _controller = null;
            _client?.Dispose();
            _client = null;
            IsConnected = false;
            SetStatus(BackendConnectionState.Disconnected, "غير متصل");
        }

        return Task.CompletedTask;
    }

    private void SetStatus(BackendConnectionState state, string message, Exception? error = null)
    {
        StatusMessage = message;
        StatusChanged?.Invoke(this, new BackendStatusEventArgs(state, message, error));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
