using DualSenseXboxBridge.Core.Mapping;

namespace DualSenseXboxBridge.Core.Settings;

public enum BackendPreference
{
    Auto = 0,
    Viiper = 1,
    ViGEm = 2,
    Null = 3
}

/// <summary>Persisted application settings. No secrets are stored here.</summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string? PreferredDevicePath { get; set; }

    public BackendPreference Backend { get; set; } = BackendPreference.Auto;

    public string ViiperHost { get; set; } = "127.0.0.1";

    public int ViiperPort { get; set; } = 3242;

    public bool ViiperAutoLaunch { get; set; }

    public string? ViiperServerPath { get; set; }

    public bool HidHideEnabled { get; set; }

    public string? HidHideDeviceInstanceId { get; set; }

    public bool AutoStart { get; set; }

    public bool StartMinimized { get; set; }

    public bool StartBridgeOnLaunch { get; set; }

    public MappingOptions Mapping { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public void Normalize()
    {
        if (SchemaVersion <= 0)
        {
            SchemaVersion = CurrentSchemaVersion;
        }

        if (string.IsNullOrWhiteSpace(ViiperHost))
        {
            ViiperHost = "127.0.0.1";
        }

        if (ViiperPort is < 1 or > 65535)
        {
            ViiperPort = 3242;
        }

        Mapping ??= new MappingOptions();
        Mapping.Normalize();
    }
}
