using System.Diagnostics;
using Microsoft.Win32;

namespace DualSenseXboxBridge.Services;

/// <summary>Per-user autostart through HKCU\...\Run. Never writes to machine-wide keys.</summary>
public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DualSenseXboxBridge";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled, bool startMinimized = false)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return;
            }

            var executable = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(executable))
            {
                return;
            }

            var command = $"\"{executable}\"" + (startMinimized ? " --minimized" : string.Empty);
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            Core.Diagnostics.AppLog.Warn("Autostart update failed: " + ex.Message);
        }
    }
}
