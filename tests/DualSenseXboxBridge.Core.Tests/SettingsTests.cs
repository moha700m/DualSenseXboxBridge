using DualSenseXboxBridge.Core.Settings;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class SettingsTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsxb-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings
        {
            Backend = BackendPreference.Viiper,
            ViiperPort = 4400,
            HidHideEnabled = true,
            Mapping = { StickDeadzone = 0.25, StickSensitivity = 1.5 }
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.True(File.Exists(path));
        Assert.Equal(BackendPreference.Viiper, loaded.Backend);
        Assert.Equal(4400, loaded.ViiperPort);
        Assert.True(loaded.HidHideEnabled);
        Assert.Equal(0.25, loaded.Mapping.StickDeadzone, 3);
        Assert.Equal(1.5, loaded.Mapping.StickSensitivity, 3);
    }

    [Fact]
    public void MissingFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsxb-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = new SettingsStore(path);

        var loaded = store.Load();

        Assert.False(loaded.AutoStart);
        Assert.Equal(BackendPreference.Auto, loaded.Backend);
        Assert.Equal(3242, loaded.ViiperPort);
    }

    [Fact]
    public void CorruptFile_FallsBackToDefaultsAndKeepsCopy()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dsxb-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ not valid json ");
        var store = new SettingsStore(path);

        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.True(File.Exists(path + ".corrupt"));
    }

    [Fact]
    public void Normalize_RepairsInvalidPort()
    {
        var settings = new AppSettings { ViiperPort = 0, ViiperHost = "  " };

        settings.Normalize();

        Assert.Equal(3242, settings.ViiperPort);
        Assert.Equal("127.0.0.1", settings.ViiperHost);
    }

    [Fact]
    public void DefaultSettings_DoNotEnableAutoStart()
    {
        var settings = AppSettings.CreateDefault();

        Assert.False(settings.AutoStart);
        Assert.False(settings.StartBridgeOnLaunch);
        Assert.False(settings.HidHideEnabled);
    }
}
