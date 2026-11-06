using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.HidHide;

namespace DualSenseXboxBridge.Services;

public sealed record HidHideState(
    bool Installed,
    bool StateKnown,
    bool Cloaked,
    IReadOnlyList<string> HiddenDevices,
    IReadOnlyList<string> WhitelistedApps);

/// <summary>
/// Safe, reversible HidHide integration through the official HidHideCLI.
/// Every change is preceded by a backup and is limited to the DualSense device and this
/// executable; no other HID device is touched and nothing is hidden without user consent.
/// </summary>
public sealed class HidHideService
{
    private const string CliFileName = "HidHideCLI.exe";

    private static readonly JsonSerializerOptions BackupOptions = new() { WriteIndented = true };

    public HidHideService()
    {
        CliPath = LocateCli(out var version);
        Version = version;
        BackupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DualSenseXboxBridge",
            "hidhide-backup.json");
    }

    public string? CliPath { get; }

    public string? Version { get; }

    public bool IsInstalled => CliPath is not null;

    public string BackupPath { get; }

    public string? LastError { get; private set; }

    public async Task<HidHideState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
        {
            return new HidHideState(false, false, false, Array.Empty<string>(), Array.Empty<string>());
        }

        // Reading the configuration normally needs elevation; the elevated path captures output
        // through a temporary script so the backup is never built from an empty read.
        var cloakedResult = await ReadAsync("--cloak-state", cancellationToken).ConfigureAwait(false);
        var devicesResult = await ReadAsync("--dev-list", cancellationToken).ConfigureAwait(false);
        var appsResult = await ReadAsync("--app-list", cancellationToken).ConfigureAwait(false);

        var known = cloakedResult.Success && devicesResult.Success && appsResult.Success;
        var cloaked = known &&
                      cloakedResult.Output.Contains("on", StringComparison.OrdinalIgnoreCase) &&
                      !cloakedResult.Output.Contains("off", StringComparison.OrdinalIgnoreCase);

        return new HidHideState(
            true,
            known,
            cloaked,
            known ? ParseList(devicesResult.Output) : Array.Empty<string>(),
            known ? ParseList(appsResult.Output) : Array.Empty<string>());
    }

    public async Task EnableAsync(string deviceInstanceId, CancellationToken cancellationToken = default)
    {
        EnsureInstalled();

        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new InvalidOperationException("تعذر تحديد مسار التطبيق الحالي.");
        }

        var before = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (!before.StateKnown)
        {
            throw new InvalidOperationException(
                "تعذر قراءة إعدادات HidHide الحالية، لذلك لن يُجرى أي تغيير. شغّل التطبيق كمسؤول ثم أعد المحاولة.");
        }

        if (!File.Exists(BackupPath))
        {
            var backup = new HidHideBackup
            {
                HiddenDeviceIds = before.HiddenDevices.ToList(),
                WhitelistedApps = before.WhitelistedApps.ToList(),
                CloakWasOn = before.Cloaked
            };

            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath)!);
            await File.WriteAllTextAsync(BackupPath, JsonSerializer.Serialize(backup, BackupOptions), cancellationToken)
                .ConfigureAwait(false);
            AppLog.Info("HidHide backup written to " + BackupPath);
        }

        // Whitelist first: cloaking before our process can see the device would break the bridge.
        await RunRequiredAsync($"--app-reg \"{executable}\"", cancellationToken).ConfigureAwait(false);
        await RunRequiredAsync($"--dev-hide \"{deviceInstanceId}\"", cancellationToken).ConfigureAwait(false);
        await RunRequiredAsync("--cloak-on", cancellationToken).ConfigureAwait(false);

        var after = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (!after.Cloaked)
        {
            throw new InvalidOperationException("HidHide لم يؤكد حالة الإخفاء. راجع إعدادات HidHide يدويًا.");
        }

        AppLog.Info($"HidHide cloaking enabled for {deviceInstanceId}");
    }

    /// <summary>
    /// Restores the exact configuration that existed before the app changed anything and
    /// removes the entries added for this app. Safe to call when HidHide is not installed.
    /// </summary>
    public async Task RestoreAsync(string? deviceInstanceId, CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
        {
            return;
        }

        var executable = Environment.ProcessPath;
        HidHideBackup? backup = null;

        try
        {
            if (File.Exists(BackupPath))
            {
                backup = JsonSerializer.Deserialize<HidHideBackup>(await File.ReadAllTextAsync(BackupPath, cancellationToken).ConfigureAwait(false));
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("HidHide backup could not be read: " + ex.Message);
        }

        var current = await GetStateAsync(cancellationToken).ConfigureAwait(false);

        // Turn cloaking off first so the physical controller is usable again immediately.
        if (backup is null || !backup.CloakWasOn)
        {
            await RunAsync("--cloak-off", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        var desiredDevices = backup?.HiddenDeviceIds ?? new List<string>();
        var devicesDiff = HidHideDiff.Compute(current.HiddenDevices, desiredDevices);
        foreach (var remove in devicesDiff.ToRemove)
        {
            await RunAsync($"--dev-unhide \"{remove}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(deviceInstanceId) &&
            !desiredDevices.Contains(deviceInstanceId, StringComparer.OrdinalIgnoreCase))
        {
            await RunAsync($"--dev-unhide \"{deviceInstanceId}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        var desiredApps = backup?.WhitelistedApps ?? new List<string>();
        var appsDiff = HidHideDiff.Compute(current.WhitelistedApps, desiredApps);
        foreach (var remove in appsDiff.ToRemove)
        {
            await RunAsync($"--app-unreg \"{remove}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(executable) &&
            !desiredApps.Contains(executable, StringComparer.OrdinalIgnoreCase))
        {
            await RunAsync($"--app-unreg \"{executable}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        foreach (var add in devicesDiff.ToAdd)
        {
            await RunAsync($"--dev-hide \"{add}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        foreach (var add in appsDiff.ToAdd)
        {
            await RunAsync($"--app-reg \"{add}\"", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        if (backup is { CloakWasOn: true })
        {
            await RunAsync("--cloak-on", elevated: true, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("HidHide backup could not be deleted: " + ex.Message);
        }

        AppLog.Info("HidHide configuration restored.");
    }

    private void EnsureInstalled()
    {
        if (!IsInstalled)
        {
            throw new InvalidOperationException(
                "HidHide غير مثبت. ثبّته من https://github.com/nefarius/HidHide ثم أعد فتح التطبيق.");
        }
    }

    private async Task<CliResult> ReadAsync(string arguments, CancellationToken cancellationToken)
    {
        var result = await RunAsync(arguments, elevated: false, cancellationToken).ConfigureAwait(false);
        if (result.Success || !string.IsNullOrWhiteSpace(result.Output))
        {
            return result;
        }

        return await RunAsync(arguments, elevated: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunRequiredAsync(string arguments, CancellationToken cancellationToken)
    {
        var result = await RunAsync(arguments, elevated: true, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException($"فشل تنفيذ HidHideCLI {arguments}: {result.Output}");
        }
    }

    private async Task<CliResult> RunAsync(string arguments, bool elevated, CancellationToken cancellationToken)
    {
        var cli = CliPath ?? throw new InvalidOperationException("HidHide غير مثبت.");

        try
        {
            if (elevated)
            {
                return await RunElevatedScriptAsync(cli, arguments, cancellationToken).ConfigureAwait(false);
            }

            var startInfo = new ProcessStartInfo(cli, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new CliResult(false, "تعذر تشغيل HidHideCLI.");
            }

            var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var standardError = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = (standardOutput + standardError).Trim();
            return new CliResult(process.ExitCode == 0, output);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            LastError = "تم إلغاء طلب صلاحيات المسؤول.";
            return new CliResult(false, LastError);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            AppLog.Warn($"HidHideCLI '{arguments}' failed: {ex.Message}");
            return new CliResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Runs HidHideCLI elevated through a temporary .cmd script. The script redirects stdout and
    /// the exit code to files so an elevated run still reports what it did; passing the arguments
    /// through a file avoids cmd.exe quoting problems with device instance ids.
    /// </summary>
    private static async Task<CliResult> RunElevatedScriptAsync(string cli, string arguments, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), "DualSenseXboxBridge");
        Directory.CreateDirectory(directory);

        var id = Guid.NewGuid().ToString("N");
        var scriptPath = Path.Combine(directory, id + ".cmd");
        var outputPath = Path.Combine(directory, id + ".out");
        var exitPath = Path.Combine(directory, id + ".code");

        var script = "@echo off\r\n" +
                     $"\"{cli}\" {arguments} > \"{outputPath}\" 2>&1\r\n" +
                     $"echo %ERRORLEVEL%> \"{exitPath}\"\r\n";

        try
        {
            await File.WriteAllTextAsync(scriptPath, script, cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo(scriptPath)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new CliResult(false, "تعذر تشغيل HidHideCLI بصلاحيات المسؤول.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = File.Exists(outputPath)
                ? (await File.ReadAllTextAsync(outputPath, cancellationToken).ConfigureAwait(false)).Trim()
                : string.Empty;

            var exitCode = 1;
            if (File.Exists(exitPath))
            {
                var text = (await File.ReadAllTextAsync(exitPath, cancellationToken).ConfigureAwait(false)).Trim();
                if (int.TryParse(text, out var parsed))
                {
                    exitCode = parsed;
                }
            }

            return new CliResult(exitCode == 0, output);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new CliResult(false, "تم إلغاء طلب صلاحيات المسؤول.");
        }
        finally
        {
            TryDelete(scriptPath);
            TryDelete(outputPath);
            TryDelete(exitPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp files are best effort.
        }
    }

    private static IReadOnlyList<string> ParseList(string output)
    {
        var results = new List<string>();
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim().Trim('"');
            if (line.Length == 0 || !line.Contains('\\'))
            {
                continue;
            }

            if (line.StartsWith("HidHide", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(line);
        }

        return results;
    }

    private static string? LocateCli(out string? version)
    {
        version = null;
        foreach (var candidate in CandidatePaths())
        {
            try
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                version = FileVersionInfo.GetVersionInfo(candidate).FileVersion;
                return candidate;
            }
            catch
            {
                // Try the next candidate.
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Path.Combine(programFiles, "Nefarius", "HidHide", "x64", CliFileName);
        yield return Path.Combine(programFiles, "Nefarius", "HidHide", CliFileName);
        yield return Path.Combine(programFiles, "HidHide", CliFileName);

        foreach (var installDirectory in ReadInstallDirectories())
        {
            yield return Path.Combine(installDirectory, "x64", CliFileName);
            yield return Path.Combine(installDirectory, CliFileName);
        }
    }

    private static IEnumerable<string> ReadInstallDirectories()
    {
        const string keyPath = @"SOFTWARE\Nefarius Software Solutions\HidHide";
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            string? directory = null;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(keyPath);
                directory = key?.GetValue("InstallDir") as string;
            }
            catch
            {
                // Missing key or access denied.
            }

            if (!string.IsNullOrWhiteSpace(directory))
            {
                yield return directory!;
            }
        }
    }

    private readonly record struct CliResult(bool Success, string Output);
}
