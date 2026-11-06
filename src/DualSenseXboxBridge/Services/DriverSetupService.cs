using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using DualSenseXboxBridge.Core.Diagnostics;

namespace DualSenseXboxBridge.Services;

public sealed record DriverInstallResult(bool Installed, int ExitCode, bool Cancelled);

/// <summary>
/// Detects the usbip-win2 driver and runs the bundled, signed upstream installer on demand.
/// Nothing is installed silently: the caller must have explicit user confirmation, and the
/// upstream installer requests administrator rights through its own manifest.
/// </summary>
public sealed class DriverSetupService
{
    private static readonly string[] InstallerPatterns = { "USBip-*-x64.exe" };

    public DriverSetupService(string? baseDirectory = null)
    {
        ThirdPartyDirectory = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "thirdparty");
    }

    public string ThirdPartyDirectory { get; }

    public string? BundledViiperPath
    {
        get
        {
            var path = Path.Combine(ThirdPartyDirectory, "viiper", "viiper.exe");
            return File.Exists(path) ? path : null;
        }
    }

    public string? BundledUsbIpInstaller
    {
        get
        {
            var directory = Path.Combine(ThirdPartyDirectory, "usbip-win2");
            try
            {
                if (!Directory.Exists(directory))
                {
                    return null;
                }

                foreach (var pattern in InstallerPatterns)
                {
                    var match = Directory.GetFiles(directory, pattern)
                        .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (match is not null)
                    {
                        return match;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn("Driver installer lookup failed: " + ex.Message);
            }

            return null;
        }
    }

    /// <summary>True when a usbip service or driver file is present.</summary>
    public bool IsUsbIpDriverInstalled => HasUsbIpService() || HasUsbIpDriverFile();

    public async Task<DriverInstallResult> InstallUsbIpAsync(CancellationToken cancellationToken = default)
    {
        var installer = BundledUsbIpInstaller
            ?? throw new InvalidOperationException(
                "مثبّت usbip-win2 غير مضمّن في هذه النسخة. نزّله من https://github.com/vadimgrn/usbip-win2");

        var startInfo = new ProcessStartInfo(installer)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(installer) ?? ThirdPartyDirectory
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException("تعذر تشغيل مثبّت usbip-win2.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var installed = IsUsbIpDriverInstalled;
            AppLog.Info($"usbip-win2 installer exited with {process.ExitCode}; driver detected: {installed}");
            return new DriverInstallResult(installed, process.ExitCode, Cancelled: false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            AppLog.Info("usbip-win2 installer was cancelled at the UAC prompt.");
            return new DriverInstallResult(false, -1, Cancelled: true);
        }
    }

    private bool HasUsbIpService()
    {
        try
        {
            using var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (services is null)
            {
                return false;
            }

            foreach (var name in services.GetSubKeyNames())
            {
                if (!name.StartsWith("usbip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var key = services.OpenSubKey(name);
                if (key?.GetValue("ImagePath") is string imagePath && imagePath.Length > 0)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("Driver service check failed: " + ex.Message);
        }

        return false;
    }

    private bool HasUsbIpDriverFile()
    {
        try
        {
            var drivers = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "drivers");

            return Directory.Exists(drivers) &&
                   Directory.GetFiles(drivers, "usbip*.sys").Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
