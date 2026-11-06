using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.Mapping;
using DualSenseXboxBridge.Core.Viiper;
using DualSenseXboxBridge.Services;

namespace DualSenseXboxBridge.Backends;

/// <summary>
/// Virtual Xbox 360 pad through VIIPER (https://github.com/Alia5/VIIPER), which emulates the
/// controller over USB/IP using the signed usbip-win2 kernel driver. Unlike ViGEmBus, VIIPER
/// is actively maintained and the device emulation lives entirely in user space.
/// </summary>
public sealed class ViiperBackend : IVirtualPadBackend
{
    private readonly string _host;
    private readonly int _port;
    private readonly string? _serverPath;
    private readonly bool _autoLaunch;

    private readonly object _writeGate = new();
    private readonly byte[] _packet = new byte[ViiperXbox360Protocol.InputPacketSize];

    private TcpClient? _streamClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _feedbackCancellation;
    private Task? _feedbackTask;
    private Process? _serverProcess;

    private volatile bool _connected;
    private string _status = "غير متصل";
    private int _busId;
    private string _deviceId = string.Empty;
    private XboxPadState _lastState;
    private long _lastWriteTicks;

    public ViiperBackend(string host, int port, string? serverPath, bool autoLaunch)
    {
        _host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
        _port = port is > 0 and < 65536 ? port : 3242;
        _serverPath = serverPath;
        _autoLaunch = autoLaunch;
    }

    public string Name => "VIIPER (USB/IP)";

    public VirtualPadBackendKind Kind => VirtualPadBackendKind.Viiper;

    public bool IsConnected => _connected;

    public string StatusMessage => _status;

    public event EventHandler<BackendStatusEventArgs>? StatusChanged;

    public event EventHandler<(byte Left, byte Right)>? RumbleReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        SetStatus(BackendConnectionState.Connecting, "جارٍ الاتصال بخادم VIIPER…");

        try
        {
            await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception first) when (_autoLaunch && TryStartServer())
        {
            AppLog.Info("VIIPER server was not reachable; started bundled server. Reason: " + first.Message);

            Exception? last = null;
            for (var attempt = 0; attempt < 12; attempt++)
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                try
                {
                    await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception retry)
                {
                    last = retry;
                }
            }

            SetStatus(BackendConnectionState.Faulted, "تعذر تشغيل خادم VIIPER: " + (last?.Message ?? first.Message), last ?? first);
            throw last ?? first;
        }
        catch (Exception ex)
        {
            var message = $"تعذر الاتصال بخادم VIIPER على {_host}:{_port}. شغّل viiper.exe أو فعّل التشغيل التلقائي من الإعدادات. ({ex.Message})";
            SetStatus(BackendConnectionState.Faulted, message, ex);
            throw new InvalidOperationException(message, ex);
        }
    }

    private async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        var version = await PingAsync(cancellationToken).ConfigureAwait(false);
        await CreateDeviceAsync(cancellationToken).ConfigureAwait(false);
        await OpenStreamAsync(cancellationToken).ConfigureAwait(false);

        _connected = true;
        SetStatus(BackendConnectionState.Connected, $"VIIPER {version} • الجهاز الافتراضي #{_deviceId}");
    }

    private async Task<string> PingAsync(CancellationToken cancellationToken)
    {
        using var document = await ViiperApi.SendAsync(_host, _port, "ping", cancellationToken).ConfigureAwait(false);
        return document.RootElement.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.String
            ? version.GetString() ?? "?"
            : "?";
    }

    private async Task CreateDeviceAsync(CancellationToken cancellationToken)
    {
        using var bus = await ViiperApi.SendAsync(_host, _port, "bus/create", cancellationToken).ConfigureAwait(false);
        _busId = bus.RootElement.GetProperty("busId").GetInt32();

        using var device = await ViiperApi.SendAsync(
            _host,
            _port,
            $"bus/{_busId}/add {{\"type\":\"xbox360\"}}",
            cancellationToken).ConfigureAwait(false);

        _deviceId = device.RootElement.TryGetProperty("devId", out var id)
            ? id.GetString() ?? "1"
            : "1";
    }

    private async Task OpenStreamAsync(CancellationToken cancellationToken)
    {
        var client = new TcpClient { NoDelay = true, SendTimeout = 1000 };
        await client.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
        var stream = client.GetStream();

        var handshake = Encoding.UTF8.GetBytes($"bus/{_busId}/{_deviceId}\0");
        await stream.WriteAsync(handshake, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        _streamClient = client;
        _stream = stream;

        _feedbackCancellation = new CancellationTokenSource();
        _feedbackTask = Task.Run(() => FeedbackLoop(stream, _feedbackCancellation.Token), CancellationToken.None);
    }

    private async Task FeedbackLoop(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[ViiperXbox360Protocol.FeedbackPacketSize];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (read == buffer.Length)
                {
                    var (left, right) = ViiperXbox360Protocol.ParseFeedback(buffer);
                    RumbleReceived?.Invoke(this, (left, right));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            AppLog.Warn("VIIPER feedback loop stopped: " + ex.Message);
        }
    }

    public void Submit(in XboxPadState state)
    {
        var stream = _stream;
        if (!_connected || stream is null)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (state == _lastState && now - _lastWriteTicks < 100)
        {
            return;
        }

        lock (_writeGate)
        {
            ViiperXbox360Protocol.WriteState(_packet, state);
            try
            {
                stream.Write(_packet, 0, _packet.Length);
                _lastState = state;
                _lastWriteTicks = now;
            }
            catch (Exception ex)
            {
                _connected = false;
                SetStatus(BackendConnectionState.Faulted, "انقطع الاتصال بالجهاز الافتراضي: " + ex.Message, ex);
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var stream = _stream;
        _stream = null;

        if (_connected && stream is not null)
        {
            try
            {
                ViiperXbox360Protocol.WriteState(_packet, XboxPadState.Neutral);
                stream.Write(_packet, 0, _packet.Length);
            }
            catch
            {
                // The stream may already be gone.
            }
        }

        _connected = false;

        try { stream?.Dispose(); } catch { }
        try { _streamClient?.Dispose(); } catch { }
        _streamClient = null;

        _feedbackCancellation?.Cancel();
        if (_feedbackTask is not null)
        {
            try
            {
                await _feedbackTask.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore shutdown races.
            }
        }

        _feedbackCancellation?.Dispose();
        _feedbackCancellation = null;
        _feedbackTask = null;

        await RemoveDeviceAsync(cancellationToken).ConfigureAwait(false);
        StopServerIfOwned();
        SetStatus(BackendConnectionState.Disconnected, "غير متصل");
    }

    private async Task RemoveDeviceAsync(CancellationToken cancellationToken)
    {
        if (_busId <= 0)
        {
            return;
        }

        var busId = _busId;
        var deviceId = _deviceId;
        _busId = 0;
        _deviceId = string.Empty;

        try
        {
            using var _ = await ViiperApi.SendAsync(_host, _port, $"bus/{busId}/remove {deviceId}", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Warn("VIIPER: could not remove device: " + ex.Message);
        }

        try
        {
            using var _ = await ViiperApi.SendAsync(_host, _port, $"bus/remove {busId}", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Warn("VIIPER: could not remove bus: " + ex.Message);
        }
    }

    private bool TryStartServer()
    {
        if (_serverProcess is not null)
        {
            return true;
        }

        var path = _serverPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            path = ViiperServerLocator.Find();
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };

            _serverProcess = Process.Start(startInfo);
            AppLog.Info("VIIPER server started from " + path);
            return _serverProcess is not null;
        }
        catch (Exception ex)
        {
            AppLog.Warn("VIIPER server could not start: " + ex.Message);
            return false;
        }
    }

    private void StopServerIfOwned()
    {
        if (_serverProcess is null)
        {
            return;
        }

        try
        {
            if (!_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("VIIPER server stop failed: " + ex.Message);
        }
        finally
        {
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }

    private void SetStatus(BackendConnectionState state, string message, Exception? error = null)
    {
        _status = message;
        StatusChanged?.Invoke(this, new BackendStatusEventArgs(state, message, error));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
