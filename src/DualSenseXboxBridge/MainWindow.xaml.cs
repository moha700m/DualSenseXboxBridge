using System.Windows;
using HidSharp;

namespace DualSenseXboxBridge;

public partial class MainWindow : Window
{
    private readonly DualSenseDiscovery _discovery = new();
    private List<HidDevice> _devices = new();
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private IVirtualPadBackend? _backend;

    public MainWindow()
    {
        InitializeComponent();
        WindowsText.Text = $"{Environment.OSVersion.VersionString} • {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}";
        RefreshDevices();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void RefreshDevices()
    {
        _devices = _discovery.FindDualSense();
        if (_devices.Count == 0)
        {
            DeviceText.Text = "No DualSense USB interface detected";
            DeviceDetail.Text = "Connect the controller by USB, then refresh.";
            HidText.Text = "No compatible Sony HID interface found.";
            return;
        }

        DeviceText.Text = $"Sony DualSense detected ({_devices.Count} HID interface(s))";
        DeviceDetail.Text = string.Join(Environment.NewLine,
            _devices.Select(d => $"VID {d.VendorID:X4} • PID {d.ProductID:X4} • Input report {d.MaxInputReportLength} bytes"));
        HidText.Text = "USB HID interface found. USB parser is enabled; Bluetooth is not enabled in this build.";
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_runTask is not null) return;
        if (_devices.Count == 0)
        {
            MessageBox.Show("Connect your DualSense by USB and press Refresh devices first.", "No controller", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "This alpha creates a virtual Xbox controller but does NOT configure HidHide. Windows/games may see both the physical DualSense and virtual Xbox pad (double input). Use this only to verify the virtual pad in joy.cpl / an XInput tester, not for gameplay. Continue?",
            "Virtual controller test", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        _runCts = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        StatusText.Text = "Starting virtual Xbox controller…";
        try
        {
            _backend = new ViGEmBackend();
            _backend.Connect();
            BackendText.Text = $"Backend: {_backend.Name} • Connected";
            var device = _devices[0];
            _runTask = Task.Run(() => PumpInput(device, _backend, _runCts.Token));
            StatusText.Text = "Running USB input → virtual Xbox. HidHide is not configured; test only.";
            await _runTask;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Stopped: " + ex.Message;
            MessageBox.Show(ex.Message, "Controller bridge error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StopBridge();
        }
    }

    private void PumpInput(HidDevice device, IVirtualPadBackend backend, CancellationToken token)
    {
        using var reader = new DualSenseInputReader(device);
        reader.Open();
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (reader.TryRead(out var state)) backend.Submit(state);
            }
            catch (TimeoutException) { }
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => StopBridge();

    private void StopBridge()
    {
        _runCts?.Cancel();
        try { _runTask?.Wait(TimeSpan.FromMilliseconds(1200)); } catch { }
        try { _backend?.Submit(new XboxPadState(0, 0, 0, 0, 0, 0, 0)); } catch { }
        try { _backend?.Disconnect(); } catch { }
        _backend?.Dispose();
        _backend = null;
        _runCts?.Dispose();
        _runCts = null;
        _runTask = null;
        BackendText.Text = "Backend: disconnected";
        StatusText.Text = "Stopped. No virtual output is active. Physical controller visibility was not changed.";
        StartButton.IsEnabled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopBridge();
        base.OnClosed(e);
    }
}