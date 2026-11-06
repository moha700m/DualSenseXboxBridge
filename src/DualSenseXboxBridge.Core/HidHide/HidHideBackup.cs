namespace DualSenseXboxBridge.Core.HidHide;

/// <summary>Snapshot of the HidHide configuration taken before the app changes anything.</summary>
public sealed class HidHideBackup
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public List<string> HiddenDeviceIds { get; set; } = new();

    public List<string> WhitelistedApps { get; set; } = new();

    public bool CloakWasOn { get; set; }
}
