using System.Diagnostics;
using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.DualSense;
using DualSenseXboxBridge.Core.Mapping;

namespace DualSenseXboxBridge.Services;

public sealed class BridgeStatusEventArgs : EventArgs
{
    public BridgeStatusEventArgs(string message, bool deviceConnected, string deviceLabel)
    {
        Message = message;
        DeviceConnected = deviceConnected;
        DeviceLabel = deviceLabel;
    }

    public string Message { get; }

    public bool DeviceConnected { get; }

    public string DeviceLabel { get; }
}

public sealed class BridgeStartOptions
{
    public string? PreferredDevicePath { get; init; }

    public required IVirtualPadBackend Backend { get; init; }

    public required Func<MappingOptions> MappingProvider { get; init; }

    public bool TestMode { get; init; }
}

/// <summary>
/// Owns the USB read loop: discovery, opening the HID interface, parsing, mapping, pushing to
/// the virtual pad, and recovery from disconnect/suspend. Runs on a dedicated background thread
/// so the UI thread never blocks on HID I/O.
/// </summary>
public sealed class BridgeService : IAsyncDisposable
{
    private readonly HidSharpDualSenseSource _source = new();
    private readonly object _lifecycleGate = new();

    private CancellationTokenSource? _cancellation;
    private Task? _pumpTask;
    private BridgeStartOptions? _options;

    private XboxPadState _current = XboxPadState.Neutral;
    private volatile bool _running;
    private string _deviceLabel = "غير متصل";
    private long _reportCounter;
    private long _lastRateTicks;
    private long _lastRateCount;
    private int _reportsPerSecond;

    public bool IsRunning => _running;

    public bool IsTestMode { get; private set; }

    public XboxPadState CurrentState => _current;

    public int ReportsPerSecond => _reportsPerSecond;

    public string DeviceLabel => _deviceLabel;

    public event EventHandler<BridgeStatusEventArgs>? StatusChanged;

    public async Task StartAsync(BridgeStartOptions options, CancellationToken cancellationToken = default)
    {
        lock (_lifecycleGate)
        {
            if (_running)
            {
                throw new InvalidOperationException("الربط يعمل بالفعل.");
            }
        }

        await options.Backend.ConnectAsync(cancellationToken).ConfigureAwait(false);

        _options = options;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _running = true;
        IsTestMode = options.TestMode;
        _lastRateTicks = Environment.TickCount64;
        _lastRateCount = 0;
        _pumpTask = Task.Run(() => Pump(options, _cancellation.Token), CancellationToken.None);
    }

    public async Task StopAsync()
    {
        var options = _options;
        var cancellation = _cancellation;

        _cancellation = null;
        _running = false;

        try
        {
            cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already gone.
        }

        var pump = _pumpTask;
        _pumpTask = null;
        if (pump is not null)
        {
            try
            {
                await pump.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                AppLog.Warn("Input pump did not stop within the timeout.");
            }
            catch
            {
                // Cancellation races are expected during shutdown.
            }
        }

        _current = XboxPadState.Neutral;
        _deviceLabel = "غير متصل";
        _reportsPerSecond = 0;

        if (options is not null)
        {
            try
            {
                options.Backend.Submit(XboxPadState.Neutral);
            }
            catch (Exception ex)
            {
                AppLog.Warn("Neutral report on stop failed: " + ex.Message);
            }

            try
            {
                await options.Backend.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppLog.Warn("Backend disconnect failed: " + ex.Message);
            }

            try
            {
                await options.Backend.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppLog.Warn("Backend dispose failed: " + ex.Message);
            }
        }

        _options = null;
        cancellation?.Dispose();
        Publish("تم إيقاف الربط. لم يعد هناك جهاز افتراضي نشط.", false);
    }

    private void Pump(BridgeStartOptions options, CancellationToken token)
    {
        var neutralSent = true;

        while (!token.IsCancellationRequested)
        {
            HidInputHandle? input = null;
            try
            {
                var descriptor = _source.FindPreferred(options.PreferredDevicePath);
                if (descriptor is null)
                {
                    if (!neutralSent)
                    {
                        SendNeutral(options);
                        neutralSent = true;
                    }

                    _deviceLabel = "غير متصل";
                    Publish("لم يتم العثور على DualSense على USB. وصّل اليد ثم اضغط تحديث.", false);
                    if (Wait(token, 1000))
                    {
                        break;
                    }

                    continue;
                }

                _deviceLabel = $"{descriptor.DisplayName} • {descriptor.VendorId:X4}:{descriptor.ProductId:X4}";
                input = _source.Open(descriptor);
                Publish($"متصل: {descriptor.DisplayName} — تقرير إدخال {input.ReportLength} بايت", true);

                var buffer = new byte[Math.Max(input.ReportLength, DualSenseIds.UsbInputReportLength)];
                var lastPresenceCheck = Environment.TickCount64;

                while (!token.IsCancellationRequested)
                {
                    var read = input.Read(buffer, 250);
                    if (read <= 0)
                    {
                        if (!neutralSent)
                        {
                            SendNeutral(options);
                            neutralSent = true;
                        }

                        var now = Environment.TickCount64;
                        if (now - lastPresenceCheck > 1500)
                        {
                            lastPresenceCheck = now;
                            if (_source.FindPreferred(options.PreferredDevicePath) is null)
                            {
                                break;
                            }
                        }

                        continue;
                    }

                    var parsed = DualSenseReportParser.ParseUsb(buffer.AsSpan(0, read));
                    if (!parsed.Success)
                    {
                        if (parsed.Failure is DualSenseParseFailure.ReportTooShort or DualSenseParseFailure.BluetoothReport)
                        {
                            AppLog.Warn("Ignored input report: " + parsed.Message);
                        }

                        continue;
                    }

                    var padState = DualSenseToXboxMapper.Map(parsed.State, options.MappingProvider());
                    _current = padState;
                    options.Backend.Submit(padState);
                    neutralSent = false;

                    Interlocked.Increment(ref _reportCounter);
                    UpdateRate();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Warn("Input pump: " + ex.Message);
                Publish("انقطع الاتصال: " + ex.Message, false);
            }
            finally
            {
                input?.Dispose();
                _current = XboxPadState.Neutral;
                _reportsPerSecond = 0;
                if (!neutralSent)
                {
                    SendNeutral(options);
                }
            }

            if (Wait(token, 1000))
            {
                break;
            }
        }
    }

    private void UpdateRate()
    {
        var now = Environment.TickCount64;
        var elapsed = now - _lastRateTicks;
        if (elapsed < 1000)
        {
            return;
        }

        var total = Interlocked.Read(ref _reportCounter);
        _reportsPerSecond = (int)((total - _lastRateCount) * 1000 / elapsed);
        _lastRateCount = total;
        _lastRateTicks = now;
    }

    private static void SendNeutral(BridgeStartOptions options)
    {
        try
        {
            options.Backend.Submit(XboxPadState.Neutral);
        }
        catch (Exception ex)
        {
            AppLog.Warn("Neutral report failed: " + ex.Message);
        }
    }

    private static bool Wait(CancellationToken token, int milliseconds) => token.WaitHandle.WaitOne(milliseconds);

    private void Publish(string message, bool deviceConnected)
    {
        StatusChanged?.Invoke(this, new BridgeStatusEventArgs(message, deviceConnected, _deviceLabel));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
