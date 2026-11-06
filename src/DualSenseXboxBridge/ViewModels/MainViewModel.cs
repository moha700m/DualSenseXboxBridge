using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.Hid;
using DualSenseXboxBridge.Core.HidHide;
using DualSenseXboxBridge.Core.Mapping;
using DualSenseXboxBridge.Core.Settings;
using DualSenseXboxBridge.Services;

namespace DualSenseXboxBridge.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsStore _store;
    private readonly HidHideService _hidHide = new();
    private readonly DriverSetupService _driverSetup = new();
    private readonly BridgeService _bridge = new();
    private readonly HidSharpDualSenseSource _source = new();
    private AppSettings _settings;

    private int _section;
    private bool _isBusy;
    private bool _isRunning;
    private bool _isTestMode;
    private bool _isControllerConnected;
    private string _controllerTitle = "جارٍ البحث عن DualSense…";
    private string _controllerDetail = "وصّل اليد بكابل USB.";
    private string _primaryStatus = "الربط متوقف. فعّل وضع Xbox للبدء.";
    private string _backendName = "لم يتم اختياره";
    private string _backendStatus = "غير متصل";
    private string _hidHideStatus = "جارٍ الفحص…";
    private string _errorText = string.Empty;
    private string _batteryText = "—";
    private string _rateText = "0 Hz";
    private string _deviceLabel = "—";
    private bool _isBackendConnected;
    private bool _isHidHideInstalled;
    private bool _isHidHideActive;
    private bool _isUsbIpDriverInstalled;
    private bool _hasBundledDriver;
    private string _driverStatus = "جارٍ فحص التعريف…";

    private bool _btnA;
    private bool _btnB;
    private bool _btnX;
    private bool _btnY;
    private bool _btnLb;
    private bool _btnRb;
    private bool _btnBack;
    private bool _btnStart;
    private bool _btnLs;
    private bool _btnRs;
    private bool _btnGuide;
    private bool _dpadUp;
    private bool _dpadDown;
    private bool _dpadLeft;
    private bool _dpadRight;
    private double _triggerLt;
    private double _triggerRt;
    private string _triggerLtText = "0%";
    private string _triggerRtText = "0%";
    private double _leftDotX = 48;
    private double _leftDotY = 48;
    private double _rightDotX = 48;
    private double _rightDotY = 48;
    private string _leftStickText = "X 0  •  Y 0";
    private string _rightStickText = "X 0  •  Y 0";

    public MainViewModel(SettingsStore store, StartupOptions startupOptions)
    {
        _store = store;
        StartupOptions = startupOptions;
        _settings = store.Load();

        NavItems = new ObservableCollection<NavItem>
        {
            new("الحالة", new RelayCommand(() => SelectSection(0))),
            new("اختبار اليد", new RelayCommand(() => SelectSection(1))),
            new("الإعدادات", new RelayCommand(() => SelectSection(2))),
            new("حول التطبيق", new RelayCommand(() => SelectSection(3)))
        };

        RefreshCommand = new RelayCommand(RefreshDevices);
        StartCommand = new AsyncRelayCommand(StartBridgeAsync);
        StopCommand = new AsyncRelayCommand(StopBridgeAsync);
        TestCommand = new AsyncRelayCommand(StartTestModeAsync);
        EnableHidHideCommand = new AsyncRelayCommand(EnableHidHideAsync);
        RestorePhysicalCommand = new AsyncRelayCommand(RestorePhysicalControllerAsync);
        InstallDriverCommand = new AsyncRelayCommand(InstallDriverAsync);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        OpenLogsCommand = new RelayCommand(OpenLogsFolder);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);

        _bridge.StatusChanged += OnBridgeStatusChanged;
        SelectSection(0);
        RefreshDriverStatus();

        _ = LoadAsync();
    }

    public ObservableCollection<NavItem> NavItems { get; }

    public StartupOptions StartupOptions { get; }

    public ICommand RefreshCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand TestCommand { get; }
    public ICommand EnableHidHideCommand { get; }
    public ICommand RestorePhysicalCommand { get; }
    public ICommand InstallDriverCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand OpenDataFolderCommand { get; }

    public string VersionText => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public string SettingsPath => _store.FilePath;

    public string DataFolder => SettingsStore.DefaultDirectory;

    public bool ShowStatus => _section == 0;
    public bool ShowTester => _section == 1;
    public bool ShowSettings => _section == 2;
    public bool ShowAbout => _section == 3;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                Raise(nameof(CanStart));
                Raise(nameof(CanStop));
                Raise(nameof(CanTest));
                Raise(nameof(CanRefresh));
                Raise(nameof(CanManageHidHide));
                Raise(nameof(CanInstallDriver));
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (Set(ref _isRunning, value))
            {
                Raise(nameof(CanStart));
                Raise(nameof(CanStop));
                Raise(nameof(CanTest));
                Raise(nameof(CanRefresh));
                Raise(nameof(CanManageHidHide));
                Raise(nameof(CanInstallDriver));
                Raise(nameof(IsIdle));
                Raise(nameof(RunStateText));
                Raise(nameof(CanInstallDriver));
            }
        }
    }

    public bool IsIdle => !IsRunning;

    public bool IsTestMode
    {
        get => _isTestMode;
        private set
        {
            if (Set(ref _isTestMode, value))
            {
                Raise(nameof(IsVirtualOutputActive));
                Raise(nameof(ModeText));
            }
        }
    }

    public bool IsVirtualOutputActive => IsRunning && !IsTestMode;

    public string ModeText => IsTestMode ? "وضع الاختبار" : "وضع Xbox";

    public string RunStateText => IsRunning ? "يعمل" : "متوقف";

    public bool CanStart => !IsRunning && !IsBusy;
    public bool CanStop => IsRunning && !IsBusy;
    public bool CanTest => !IsRunning && !IsBusy;

    public bool CanRefresh => !IsBusy;

    public bool CanManageHidHide => !IsBusy;

    public bool IsControllerConnected
    {
        get => _isControllerConnected;
        private set
        {
            if (Set(ref _isControllerConnected, value))
            {
                Raise(nameof(ControllerBadgeText));
            }
        }
    }

    public string ControllerBadgeText => IsControllerConnected ? "متصل" : "غير متصل";

    public string ControllerTitle
    {
        get => _controllerTitle;
        private set => Set(ref _controllerTitle, value);
    }

    public string ControllerDetail
    {
        get => _controllerDetail;
        private set => Set(ref _controllerDetail, value);
    }

    public string PrimaryStatus
    {
        get => _primaryStatus;
        private set => Set(ref _primaryStatus, value);
    }

    public string BackendName
    {
        get => _backendName;
        private set => Set(ref _backendName, value);
    }

    public string BackendStatus
    {
        get => _backendStatus;
        private set => Set(ref _backendStatus, value);
    }

    public bool IsBackendConnected
    {
        get => _isBackendConnected;
        private set => Set(ref _isBackendConnected, value);
    }

    public string DeviceLabel
    {
        get => _deviceLabel;
        private set => Set(ref _deviceLabel, value);
    }

    public string BatteryText
    {
        get => _batteryText;
        private set => Set(ref _batteryText, value);
    }

    public string RateText
    {
        get => _rateText;
        private set => Set(ref _rateText, value);
    }

    public string HidHideStatus
    {
        get => _hidHideStatus;
        private set => Set(ref _hidHideStatus, value);
    }

    public bool IsHidHideInstalled
    {
        get => _isHidHideInstalled;
        private set => Set(ref _isHidHideInstalled, value);
    }

    public bool IsHidHideActive
    {
        get => _isHidHideActive;
        private set => Set(ref _isHidHideActive, value);
    }

    public string ErrorText
    {
        get => _errorText;
        private set
        {
            if (Set(ref _errorText, value))
            {
                Raise(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public bool IsUsbIpDriverInstalled
    {
        get => _isUsbIpDriverInstalled;
        private set
        {
            if (Set(ref _isUsbIpDriverInstalled, value))
            {
                Raise(nameof(CanInstallDriver));
            }
        }
    }

    public bool HasBundledDriver
    {
        get => _hasBundledDriver;
        private set
        {
            if (Set(ref _hasBundledDriver, value))
            {
                Raise(nameof(CanInstallDriver));
            }
        }
    }

    public string DriverStatus
    {
        get => _driverStatus;
        private set => Set(ref _driverStatus, value);
    }

    public bool CanInstallDriver => !IsBusy && !IsRunning && !IsUsbIpDriverInstalled && HasBundledDriver;

    // ---- Tester -------------------------------------------------------------

    public bool BtnA { get => _btnA; private set => Set(ref _btnA, value); }
    public bool BtnB { get => _btnB; private set => Set(ref _btnB, value); }
    public bool BtnX { get => _btnX; private set => Set(ref _btnX, value); }
    public bool BtnY { get => _btnY; private set => Set(ref _btnY, value); }
    public bool BtnLb { get => _btnLb; private set => Set(ref _btnLb, value); }
    public bool BtnRb { get => _btnRb; private set => Set(ref _btnRb, value); }
    public bool BtnBack { get => _btnBack; private set => Set(ref _btnBack, value); }
    public bool BtnStart { get => _btnStart; private set => Set(ref _btnStart, value); }
    public bool BtnLs { get => _btnLs; private set => Set(ref _btnLs, value); }
    public bool BtnRs { get => _btnRs; private set => Set(ref _btnRs, value); }
    public bool BtnGuide { get => _btnGuide; private set => Set(ref _btnGuide, value); }
    public bool DpadUp { get => _dpadUp; private set => Set(ref _dpadUp, value); }
    public bool DpadDown { get => _dpadDown; private set => Set(ref _dpadDown, value); }
    public bool DpadLeft { get => _dpadLeft; private set => Set(ref _dpadLeft, value); }
    public bool DpadRight { get => _dpadRight; private set => Set(ref _dpadRight, value); }

    public double TriggerLt { get => _triggerLt; private set => Set(ref _triggerLt, value); }
    public double TriggerRt { get => _triggerRt; private set => Set(ref _triggerRt, value); }
    public string TriggerLtText { get => _triggerLtText; private set => Set(ref _triggerLtText, value); }
    public string TriggerRtText { get => _triggerRtText; private set => Set(ref _triggerRtText, value); }

    public double LeftDotX { get => _leftDotX; private set => Set(ref _leftDotX, value); }
    public double LeftDotY { get => _leftDotY; private set => Set(ref _leftDotY, value); }
    public double RightDotX { get => _rightDotX; private set => Set(ref _rightDotX, value); }
    public double RightDotY { get => _rightDotY; private set => Set(ref _rightDotY, value); }

    public string LeftStickText { get => _leftStickText; private set => Set(ref _leftStickText, value); }
    public string RightStickText { get => _rightStickText; private set => Set(ref _rightStickText, value); }

    // ---- Settings -----------------------------------------------------------

    public double StickDeadzonePercent
    {
        get => Math.Round(_settings.Mapping.StickDeadzone * 100, 0);
        set
        {
            _settings.Mapping.StickDeadzone = Math.Clamp(value / 100.0, 0, 0.9);
            Raise();
            Raise(nameof(StickDeadzoneLabel));
        }
    }

    public string StickDeadzoneLabel => $"{StickDeadzonePercent:0}%";

    public double TriggerDeadzonePercent
    {
        get => Math.Round(_settings.Mapping.TriggerDeadzone * 100, 0);
        set
        {
            _settings.Mapping.TriggerDeadzone = Math.Clamp(value / 100.0, 0, 0.9);
            Raise();
            Raise(nameof(TriggerDeadzoneLabel));
        }
    }

    public string TriggerDeadzoneLabel => $"{TriggerDeadzonePercent:0}%";

    public double SensitivityPercent
    {
        get => Math.Round(_settings.Mapping.StickSensitivity * 100, 0);
        set
        {
            _settings.Mapping.StickSensitivity = Math.Clamp(value / 100.0, 0.2, 3.0);
            Raise();
            Raise(nameof(SensitivityLabel));
        }
    }

    public string SensitivityLabel => $"{SensitivityPercent:0}%";

    public bool InvertLeftX
    {
        get => _settings.Mapping.InvertLeftX;
        set
        {
            _settings.Mapping.InvertLeftX = value;
            Raise();
        }
    }

    public bool InvertLeftY
    {
        get => _settings.Mapping.InvertLeftY;
        set
        {
            _settings.Mapping.InvertLeftY = value;
            Raise();
        }
    }

    public bool InvertRightX
    {
        get => _settings.Mapping.InvertRightX;
        set
        {
            _settings.Mapping.InvertRightX = value;
            Raise();
        }
    }

    public bool InvertRightY
    {
        get => _settings.Mapping.InvertRightY;
        set
        {
            _settings.Mapping.InvertRightY = value;
            Raise();
        }
    }

    public bool SwapSticks
    {
        get => _settings.Mapping.SwapSticks;
        set
        {
            _settings.Mapping.SwapSticks = value;
            Raise();
        }
    }

    public bool AutoStart
    {
        get => _settings.AutoStart;
        set
        {
            _settings.AutoStart = value;
            Raise();
        }
    }

    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set
        {
            _settings.StartMinimized = value;
            Raise();
        }
    }

    public bool StartBridgeOnLaunch
    {
        get => _settings.StartBridgeOnLaunch;
        set
        {
            _settings.StartBridgeOnLaunch = value;
            Raise();
        }
    }

    public string ViiperEndpoint
    {
        get => $"{_settings.ViiperHost}:{_settings.ViiperPort}";
        set
        {
            var text = (value ?? string.Empty).Trim();
            var separator = text.LastIndexOf(':');
            if (separator > 0 && int.TryParse(text[(separator + 1)..], out var port) && port is > 0 and < 65536)
            {
                _settings.ViiperHost = text[..separator].Trim();
                _settings.ViiperPort = port;
                Raise();
            }
        }
    }

    public string ViiperServerPath
    {
        get => _settings.ViiperServerPath ?? string.Empty;
        set
        {
            _settings.ViiperServerPath = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            Raise();
        }
    }

    public bool ViiperAutoLaunch
    {
        get => _settings.ViiperAutoLaunch;
        set
        {
            _settings.ViiperAutoLaunch = value;
            Raise();
        }
    }

    public string SettingsSavedText { get; private set; } = string.Empty;

    // ---- Commands -----------------------------------------------------------

    private void SelectSection(int index)
    {
        _section = index;
        for (var i = 0; i < NavItems.Count; i++)
        {
            NavItems[i].IsActive = i == index;
        }

        Raise(nameof(ShowStatus));
        Raise(nameof(ShowTester));
        Raise(nameof(ShowSettings));
        Raise(nameof(ShowAbout));
    }

    private async Task LoadAsync()
    {
        RefreshDevices();
        await RefreshHidHideAsync().ConfigureAwait(true);
        RefreshSettingsFromStore();

        if (_settings.StartBridgeOnLaunch || StartupOptions.Minimized)
        {
            AppLog.Info("StartBridgeOnLaunch is enabled; starting bridge.");
            await StartBridgeAsync().ConfigureAwait(true);
        }
    }

    public void RefreshDevices()
    {
        ErrorText = string.Empty;

        try
        {
            var all = _source.Enumerate();
            var candidates = all.Where(d => DualSenseDeviceFilter.IsCandidate(d, out _)).ToList();

            IsControllerConnected = candidates.Count > 0;
            if (candidates.Count == 0)
            {
                ControllerTitle = "لم يتم العثور على DualSense";
                ControllerDetail = "وصّل اليد عبر USB. إن كانت موصولة بالفعل فأغلق أي برنامج يسيطر عليها ثم أعد المحاولة.";
                DeviceLabel = "—";
                BatteryText = "—";
                return;
            }

            var selected = DualSenseDeviceFilter.Select(candidates, _settings.PreferredDevicePath) ?? candidates[0];
            ControllerTitle = selected.DisplayName;
            ControllerDetail =
                $"VID {selected.VendorId:X4} • PID {selected.ProductId:X4} • تقرير {selected.MaxInputReportLength} بايت • USB";
            DeviceLabel = selected.DevicePath;
            BatteryText = "تُقرأ أثناء التشغيل";

            AppLog.Info($"Detected {selected.DisplayName} at {selected.DevicePath}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("Device refresh failed", ex);
        }
    }

    private async Task StartBridgeAsync()
    {
        if (IsRunning || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorText = string.Empty;

        IVirtualPadBackend? backend = null;
        try
        {
            RefreshDevices();

            var descriptor = _source.FindPreferred(_settings.PreferredDevicePath);
            if (descriptor is null || !IsControllerConnected)
            {
                ErrorText = "لا توجد يد DualSense متصلة عبر USB. وصّلها ثم أعد المحاولة.";
                return;
            }

            _settings.PreferredDevicePath = descriptor.DevicePath;
            backend = BackendFactory.Create(_settings);
            backend.StatusChanged += OnBackendStatusChanged;
            BackendName = backend.Name;

            if (backend.Kind == VirtualPadBackendKind.Viiper && !_driverSetup.IsUsbIpDriverInstalled)
            {
                var proceed = MessageBox.Show(
                    "تعريف usbip-win2 غير مثبت، لذلك غالبًا لن يظهر جهاز Xbox الافتراضي في الألعاب.\n\n" +
                    "يمكنك تثبيته من «الإعدادات ← تعريف USB/IP» أو من مثبّت الحزمة.\n\nهل تريد المتابعة على أي حال؟",
                    "التعريف غير مثبت",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (proceed != MessageBoxResult.Yes)
                {
                    await backend.DisposeAsync().ConfigureAwait(true);
                    PrimaryStatus = "تم الإلغاء: ثبّت تعريف usbip-win2 أولًا.";
                    return;
                }
            }
            BackendStatus = "جارٍ التوصيل…";
            PrimaryStatus = $"جارٍ توصيل {backend.Name}…";

            var options = new BridgeStartOptions
            {
                Backend = backend,
                PreferredDevicePath = descriptor.DevicePath,
                MappingProvider = () => _settings.Mapping,
                TestMode = false
            };

            await _bridge.StartAsync(options).ConfigureAwait(true);

            IsRunning = true;
            IsTestMode = false;
            IsBackendConnected = backend.IsConnected;
            BackendStatus = backend.StatusMessage;
            PrimaryStatus = "وضع Xbox مفعّل: اليد تعمل كجهاز Xbox 360 افتراضي.";
            AppLog.Info("Bridge started with backend " + backend.Name);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            PrimaryStatus = "تعذر تشغيل وضع Xbox.";
            BackendStatus = "فشل الاتصال";
            AppLog.Error("Bridge start failed", ex);

            if (backend is not null)
            {
                try
                {
                    await backend.DisposeAsync().ConfigureAwait(true);
                }
                catch
                {
                    // Ignore cleanup errors after a failed start.
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartTestModeAsync()
    {
        if (IsRunning || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            RefreshDevices();
            if (!IsControllerConnected)
            {
                ErrorText = "وصّل اليد عبر USB أولًا لتشغيل الاختبار.";
                return;
            }

            var backend = new Backends.NullPadBackend();
            var options = new BridgeStartOptions
            {
                Backend = backend,
                PreferredDevicePath = _settings.PreferredDevicePath,
                MappingProvider = () => _settings.Mapping,
                TestMode = true
            };

            await _bridge.StartAsync(options).ConfigureAwait(true);
            IsRunning = true;
            IsTestMode = true;
            IsBackendConnected = true;
            BackendName = backend.Name;
            BackendStatus = backend.StatusMessage;
            PrimaryStatus = "وضع الاختبار: تُقرأ اليد دون إنشاء جهاز Xbox افتراضي.";
            SelectSection(1);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("Test mode failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StopBridgeAsync()
    {
        if (!IsRunning || IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _bridge.StopAsync().ConfigureAwait(true);
            IsRunning = false;
            IsTestMode = false;
            IsBackendConnected = false;
            BackendStatus = "غير متصل";
            PrimaryStatus = "تم إيقاف الربط وأُرسلت حالة محايدة. الجهاز الافتراضي لم يعد نشطًا.";
            RateText = "0 Hz";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("Bridge stop failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdateLiveState()
    {
        var state = _bridge.CurrentState;

        BtnA = state.IsPressed(Xbox360Buttons.A);
        BtnB = state.IsPressed(Xbox360Buttons.B);
        BtnX = state.IsPressed(Xbox360Buttons.X);
        BtnY = state.IsPressed(Xbox360Buttons.Y);
        BtnLb = state.IsPressed(Xbox360Buttons.LeftShoulder);
        BtnRb = state.IsPressed(Xbox360Buttons.RightShoulder);
        BtnBack = state.IsPressed(Xbox360Buttons.Back);
        BtnStart = state.IsPressed(Xbox360Buttons.Start);
        BtnLs = state.IsPressed(Xbox360Buttons.LeftThumb);
        BtnRs = state.IsPressed(Xbox360Buttons.RightThumb);
        BtnGuide = state.IsPressed(Xbox360Buttons.Guide);
        DpadUp = state.IsPressed(Xbox360Buttons.DpadUp);
        DpadDown = state.IsPressed(Xbox360Buttons.DpadDown);
        DpadLeft = state.IsPressed(Xbox360Buttons.DpadLeft);
        DpadRight = state.IsPressed(Xbox360Buttons.DpadRight);

        TriggerLt = state.LeftTrigger;
        TriggerRt = state.RightTrigger;
        TriggerLtText = $"{state.LeftTrigger * 100 / 255}%";
        TriggerRtText = $"{state.RightTrigger * 100 / 255}%";

        LeftDotX = Dot(state.LeftStickX, isX: true);
        LeftDotY = Dot(state.LeftStickY, isX: false);
        RightDotX = Dot(state.RightStickX, isX: true);
        RightDotY = Dot(state.RightStickY, isX: false);

        LeftStickText = $"X {AxisMath.ToPercent(state.LeftStickX):+0;-0;0}  •  Y {AxisMath.ToPercent(state.LeftStickY):+0;-0;0}";
        RightStickText = $"X {AxisMath.ToPercent(state.RightStickX):+0;-0;0}  •  Y {AxisMath.ToPercent(state.RightStickY):+0;-0;0}";

        RateText = IsRunning ? $"{_bridge.ReportsPerSecond} Hz" : "0 Hz";

        if (IsRunning)
        {
            BackendStatus = IsTestMode ? BackendStatus : _bridge.DeviceLabel;
        }
    }

    private static double Dot(short value, bool isX)
    {
        const double center = 55;
        const double radius = 40;
        const double dot = 7;
        var offset = value / 32767.0 * radius;
        return isX ? center + offset - dot : center - offset - dot;
    }

    private void RefreshDriverStatus()
    {
        try
        {
            HasBundledDriver = _driverSetup.BundledUsbIpInstaller is not null;
            IsUsbIpDriverInstalled = _driverSetup.IsUsbIpDriverInstalled;
            DriverStatus = IsUsbIpDriverInstalled
                ? "تعريف usbip-win2 مثبت وجاهز."
                : HasBundledDriver
                    ? "غير مثبت — المثبّت الرسمي الموقّع مضمّن مع التطبيق."
                    : "غير مثبت — لم يُعثر على المثبّت المضمّن.";
        }
        catch (Exception ex)
        {
            DriverStatus = "تعذر فحص حالة التعريف.";
            AppLog.Warn("Driver status check failed: " + ex.Message);
        }

        Raise(nameof(CanInstallDriver));
    }

    private async Task InstallDriverAsync()
    {
        if (!CanInstallDriver)
        {
            return;
        }

        var installer = _driverSetup.BundledUsbIpInstaller;
        if (installer is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "سيتم تشغيل مثبّت تعريف usbip-win2 الرسمي الموقّع المضمّن مع التطبيق.\n\n" +
            $"• الملف: {Path.GetFileName(installer)}\n" +
            "• يتطلب صلاحيات مسؤول (UAC).\n" +
            "• قد يطلب المثبّت إعادة تشغيل Windows.\n\nهل تريد المتابعة؟",
            "تثبيت تعريف USB/IP",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            var result = await _driverSetup.InstallUsbIpAsync().ConfigureAwait(true);
            RefreshDriverStatus();

            if (result.Cancelled)
            {
                ErrorText = "تم إلغاء تثبيت التعريف.";
            }
            else if (result.Installed)
            {
                PrimaryStatus = "تم تثبيت تعريف usbip-win2. أعد تشغيل Windows إن طلب المثبّت.";
            }
            else
            {
                ErrorText = $"انتهى المثبّت بالرمز {result.ExitCode} ولم يُكتشف التعريف. أعد التشغيل ثم أعد المحاولة.";
            }
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("Driver installation failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshHidHideAsync()
    {
        try
        {
            var state = await _hidHide.GetStateAsync().ConfigureAwait(true);
            IsHidHideInstalled = state.Installed;
            IsHidHideActive = state.Cloaked;
            HidHideStatus = !state.Installed
                ? "غير مثبت — لن يتم إخفاء اليد الفعلية (قد يحدث Double Input)."
                : !state.StateKnown
                    ? "مثبت — تعذر قراءة الحالة الحالية (تتطلب صلاحيات مسؤول)."
                    : state.Cloaked
                    ? $"مفعّل — {state.HiddenDevices.Count} جهاز مخفي و{state.WhitelistedApps.Count} تطبيق مسموح."
                    : "مثبت — غير مفعّل.";
        }
        catch (Exception ex)
        {
            HidHideStatus = "تعذر قراءة حالة HidHide.";
            AppLog.Warn("HidHide state read failed: " + ex.Message);
        }
    }

    private async Task EnableHidHideAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var descriptor = _source.FindPreferred(_settings.PreferredDevicePath);
        if (descriptor is null)
        {
            ErrorText = "وصّل اليد أولًا قبل تفعيل HidHide.";
            return;
        }

        if (!HidHideDevicePath.TryToDeviceInstanceId(descriptor.DevicePath, out var instanceId))
        {
            ErrorText = "تعذر استخراج معرّف الجهاز المطلوب لـ HidHide.";
            return;
        }

        var confirm = MessageBox.Show(
            "سيفعل التطبيق HidHide على يد DualSense هذه فقط:\n\n" +
            $"• إضافة التطبيق إلى قائمة التطبيقات المسموح لها.\n• إخفاء الجهاز: {instanceId}\n\n" +
            "قد تظهر نافذة صلاحيات المسؤول (UAC). سيتم حفظ نسخة احتياطية من إعداداتك الحالية، " +
            "وستُستعاد تلقائيًا عند إيقاف الميزة أو إغلاق التطبيق.\n\nهل تريد المتابعة؟",
            "تفعيل HidHide — تأكيد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            await _hidHide.EnableAsync(instanceId).ConfigureAwait(true);
            _settings.HidHideEnabled = true;
            _settings.HidHideDeviceInstanceId = instanceId;
            _store.Save(_settings);
            await RefreshHidHideAsync().ConfigureAwait(true);
            PrimaryStatus = "HidHide مفعّل: اليد الفعلية مخفية عن الألعاب، والجهاز الافتراضي فقط هو الظاهر.";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("HidHide enable failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestorePhysicalControllerAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "سيتم إيقاف إخفاء اليد وإزالة هذا التطبيق من قائمة HidHide، مع استعادة الإعدادات السابقة.\n\nهل تريد المتابعة؟",
            "استعادة اليد الفعلية",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _hidHide.RestoreAsync(_settings.HidHideDeviceInstanceId).ConfigureAwait(true);
            _settings.HidHideEnabled = false;
            _settings.HidHideDeviceInstanceId = null;
            _store.Save(_settings);
            await RefreshHidHideAsync().ConfigureAwait(true);
            PrimaryStatus = "تمت استعادة اليد الفعلية وإعدادات HidHide السابقة.";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("HidHide restore failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settings.Normalize();
            _store.Save(_settings);
            AutoStartService.SetEnabled(_settings.AutoStart, _settings.StartMinimized);
            SettingsSavedText = $"تم الحفظ في {_store.FilePath}";
            Raise(nameof(SettingsSavedText));
            PrimaryStatus = "تم حفظ الإعدادات.";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            AppLog.Error("Settings save failed", ex);
        }
    }

    private void RefreshSettingsFromStore()
    {
        _settings = _store.Load();
        Raise(nameof(StickDeadzonePercent));
        Raise(nameof(TriggerDeadzonePercent));
        Raise(nameof(SensitivityPercent));
        Raise(nameof(InvertLeftX));
        Raise(nameof(InvertLeftY));
        Raise(nameof(InvertRightX));
        Raise(nameof(InvertRightY));
        Raise(nameof(SwapSticks));
        Raise(nameof(AutoStart));
        Raise(nameof(StartMinimized));
        Raise(nameof(StartBridgeOnLaunch));
        Raise(nameof(ViiperEndpoint));
        Raise(nameof(ViiperServerPath));
        Raise(nameof(ViiperAutoLaunch));
    }

    private void OpenLogsFolder() => OpenFolder(AppLog.Directory);

    private void OpenDataFolder() => OpenFolder(SettingsStore.DefaultDirectory);

    private static void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppLog.Warn("Could not open folder: " + ex.Message);
        }
    }

    private void OnBackendStatusChanged(object? sender, BackendStatusEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyBackendStatus(e);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => ApplyBackendStatus(e)));
    }

    private void ApplyBackendStatus(BackendStatusEventArgs e)
    {
        BackendStatus = e.Message;
        IsBackendConnected = e.State == BackendConnectionState.Connected;

        if (e.State == BackendConnectionState.Faulted)
        {
            ErrorText = e.Message;
            PrimaryStatus = "توقف الإرسال إلى الجهاز الافتراضي. راجع حالة الـ Backend ثم أعد التشغيل.";
            AppLog.Warn("Backend faulted: " + e.Message);
        }
    }

    private void OnBridgeStatusChanged(object? sender, BridgeStatusEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyBridgeStatus(e);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => ApplyBridgeStatus(e)));
    }

    private void ApplyBridgeStatus(BridgeStatusEventArgs e)
    {
        PrimaryStatus = e.Message;
        IsControllerConnected = e.DeviceConnected;
        if (!string.IsNullOrWhiteSpace(e.DeviceLabel))
        {
            ControllerTitle = e.DeviceLabel;
        }
    }

    public async Task ShutdownAsync()
    {
        try
        {
            if (IsRunning)
            {
                await _bridge.StopAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("Bridge stop during shutdown failed: " + ex.Message);
        }

        if (_settings.HidHideEnabled)
        {
            try
            {
                await _hidHide.RestoreAsync(_settings.HidHideDeviceInstanceId).ConfigureAwait(true);
                _settings.HidHideEnabled = false;
                _settings.HidHideDeviceInstanceId = null;
                _store.Save(_settings);
            }
            catch (Exception ex)
            {
                AppLog.Error("HidHide restore during shutdown failed", ex);
            }
        }

        try
        {
            _store.Save(_settings);
        }
        catch (Exception ex)
        {
            AppLog.Warn("Settings save on shutdown failed: " + ex.Message);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(propertyName);
        return true;
    }

    private void Raise([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
}
