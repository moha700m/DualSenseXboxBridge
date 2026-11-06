using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.Settings;
using DualSenseXboxBridge.Services;
using DualSenseXboxBridge.ViewModels;

namespace DualSenseXboxBridge;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _liveTimer;
    private bool _shutdownComplete;

    public MainWindow()
    {
        InitializeComponent();

        var options = StartupOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
        _viewModel = new MainViewModel(new SettingsStore(), options);
        DataContext = _viewModel;

        _liveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _liveTimer.Tick += OnLiveTimerTick;
        _liveTimer.Start();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.StartupOptions.Minimized || _viewModel.StartMinimized)
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void OnLiveTimerTick(object? sender, EventArgs e)
    {
        if (_viewModel.IsRunning)
        {
            _viewModel.UpdateLiveState();
        }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_shutdownComplete)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _liveTimer.Stop();

        try
        {
            await _viewModel.ShutdownAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("Shutdown failed", ex);
        }

        _shutdownComplete = true;
        Close();
    }
}
