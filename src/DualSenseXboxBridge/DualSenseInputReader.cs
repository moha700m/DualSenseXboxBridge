using HidSharp;

namespace DualSenseXboxBridge;

public sealed class DualSenseInputReader : IDisposable
{
    private HidStream? _stream;
    public HidDevice Device { get; }

    public DualSenseInputReader(HidDevice device) => Device = device;

    public void Open()
    {
        if (!Device.TryOpen(out var stream))
            throw new IOException("Windows could not open the DualSense HID interface. Close other controller tools and retry.");
        _stream = stream;
        _stream.ReadTimeout = 1000;
    }

    public bool TryRead(out XboxPadState state)
    {
        state = new XboxPadState(0, 0, 0, 0, 0, 0, 0);
        if (_stream is null) throw new InvalidOperationException("Input reader is not open.");
        var report = new byte[Device.MaxInputReportLength];
        int count = _stream.Read(report, 0, report.Length);
        return count >= 64 && DualSenseReportParser.TryParseUsb(report.AsSpan(0, count), out state);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
