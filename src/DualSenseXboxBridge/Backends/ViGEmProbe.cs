using Microsoft.Win32;

namespace DualSenseXboxBridge.Backends;

/// <summary>Checks whether the ViGEmBus kernel service is installed, without opening the bus.</summary>
public static class ViGEmProbe
{
    public static bool IsDriverInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus");
            return key is not null;
        }
        catch
        {
            return false;
        }
    }
}
