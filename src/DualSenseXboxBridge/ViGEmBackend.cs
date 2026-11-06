using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace DualSenseXboxBridge;

// Legacy output backend. Requires a separately installed, signed ViGEmBus driver.
// ViGEm is retired; keep this adapter replaceable and do not install drivers automatically.
public sealed class ViGEmBackend : IVirtualPadBackend
{
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;
    public string Name => "ViGEmBus (legacy / optional)";
    public bool IsConnected => _controller is not null;

    public void Connect()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.Connect();
    }

    public void Submit(XboxPadState state)
    {
        if (_controller is null) throw new InvalidOperationException("Virtual controller is not connected.");
        _controller.SetButtonsFull(state.Buttons);
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.LeftTrigger);
        _controller.SetSliderValue(Xbox360Slider.RightTrigger, state.RightTrigger);
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.LeftX);
        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.LeftY);
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, state.RightX);
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, state.RightY);
        _controller.SubmitReport();
    }

    public void Disconnect()
    {
        try { _controller?.Disconnect(); }
        finally { _controller = null; _client?.Dispose(); _client = null; }
    }

    public void Dispose() => Disconnect();
}
