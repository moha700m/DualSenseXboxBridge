using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
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
///
/// Safety rules enforced here:
///  * only the selected DualSense device instance and this executable are touched;
///  * a backup of the previous configuration is taken *inside the same elevated batch* that
///    applies the change, so it can never be built from a failed read;
///  * nothing is hidden without an explicit user confirmation in the UI;
///  * restore only undoes what this app added, then re-applies the backup.
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

    /// <summary>
    /// Reads the current configuration without elevation. Reading may be denied by the driver;
    /// in that case the state is reported as unknown and the UI says so instead of guessing.
    /// </summary>
    public async Task<HidHideState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
        {
            return new HidHideState(false, false, false, Array.Empty<string>(), Array.Empty<string>());
        }

        var cloak = await RunDirectAsync("--cloak-state", cancellationToken).ConfigureAwait(false);
        var devices = await RunDirectAsync("--dev-list", cancellationToken).ConfigureAwait(false);
        var apps = await RunDirectAsync("--app-list", cancellationToken).ConfigureAwait(false);

        var known = cloak.Success && devices.Success && apps.Success && !string.IsNullOrWhiteSpace(cloak.Output);
        var cloaked = known && IsOn(cloak.Output);

        return new HidHideState(
            true,
            known,
            cloaked,
            known ? ParseList(devices.Output) : Array.Empty<string>(),
            known ? ParseList(apps.Output) : Array.Empty<string>());
    }

    /// <summary>
    /// Whitelists this app, hides the given device and turns cloaking on - in one elevated batch,
    /// so the user sees a single UAC prompt and the backup is captured in that same batch.
    /// </summary>
    public async Task EnableAsync(string deviceInstanceId, CancellationToken cancellationToken = default)
    {
        EnsureInstalled();

        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new InvalidOperationException("تعذر تحديد مسار التطبيق الحالي.");
        }

        var commands = new List<string>
        {
            "--cloak-state",
            "--dev-list",
            "--app-list",
            $"--app-reg \"{executable}\"",
            $"--dev-hide \"{deviceInstanceId}\"",
            "--cloak-on",
            "--cloak-state"
        };

        var results = await RunElevatedBatchAsync(commands, cancellationToken).ConfigureAwait(false);
        if (results.Count != commands.Count)
        {
            throw new InvalidOperationException("لم يكتمل تنفيذ أوامر HidHide. لم يتم تأكيد أي تغيير.");
        }

        var applyFailed = results[3].Success && results[4].Success && results[5].Success ? null : results[3].Output;
        if (applyFailed is not null)
        {
            throw new InvalidOperationException(
                "فشل تفعيل HidHide. قد يكون الطلب أُلغي أو أن التعريف غير مثبت بشكل صحيح.\n" + applyFailed);
        }

        var beforeCloak = results[0];
        var beforeDevices = results[1];
        var beforeApps = results[2];

        if (beforeCloak.Success && beforeDevices.Success && beforeApps.Success && !string.IsNullOrWhiteSpace(beforeCloak.Output))
        {
            var backup = new HidHideBackup
            {
                HiddenDeviceIds = ParseList(beforeDevices.Output).ToList(),
                WhitelistedApps = ParseList(beforeApps.Output).ToList(),
                CloakWasOn = IsOn(beforeCloak.Output)
            };

            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath)!);
            await File.WriteAllTextAsync(BackupPath, JsonSerializer.Serialize(backup, BackupOptions), cancellationToken)
                .ConfigureAwait(false);
            AppLog.Info("HidHide backup written to " + BackupPath);
        }
        else
        {
            // Restore remains safe: it only removes the entries added below.
            AppLog.Warn("HidHide previous configuration could not be read; restore will undo this app's changes only.");
        }

        if (!IsOn(results[6].Output))
        {
            throw new InvalidOperationException("HidHide لم يؤكد حالة الإخفاء بعد التفعيل. راجع إعدادات HidHide يدويًا.");
        }

        AppLog.Info($"HidHide cloaking enabled for {deviceInstanceId}");
    }

    /// <summary>
    /// Removes only the entries this app added and re-applies the saved backup.
    /// Uses a single elevated batch. Safe to call when HidHide is not installed.
    /// </summary>
    public async Task RestoreAsync(string? deviceInstanceId, CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
        {
            return;
        }

        var executable = Environment.ProcessPath;
        var backup = LoadBackup();
        var commands = new List<string>();

        if (backup is not { CloakWasOn: true })
        {
            commands.Add("--cloak-off");
        }

        if (!string.IsNullOrWhiteSpace(deviceInstanceId) &&
            (backup is null || !Contains(backup.HiddenDeviceIds, deviceInstanceId)))
        {
            commands.Add($"--dev-unhide \"{deviceInstanceId}\"");
        }

        if (!string.IsNullOrWhiteSpace(executable) &&
            (backup is null || !Contains(backup.WhitelistedApps, executable)))
        {
            commands.Add($"--app-unreg \"{executable}\"");
        }

        if (backup is not null)
        {
            commands.AddRange(backup.HiddenDeviceIds.Select(id => $"--dev-hide \"{id}\""));
            commands.AddRange(backup.WhitelistedApps.Select(app => $"--app-reg \"{app}\""));
            if (backup.CloakWasOn)
            {
                commands.Add("--cloak-on");
            }
        }

        if (commands.Count > 0)
        {
            await RunElevatedBatchAsync(commands, cancellationToken).ConfigureAwait(false);
        }

        TryDelete(BackupPath);
        AppLog.Info("HidHide configuration restored.");
    }

    private HidHideBackup? LoadBackup()
    {
        try
        {
            if (!File.Exists(BackupPath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<HidHideBackup>(File.ReadAllText(BackupPath));
        }
        catch (Exception ex)
        {
            AppLog.Warn("HidHide backup could not be read: " + ex.Message);
            return null;
        }
    }

    private static bool Contains(IEnumerable<string> values, string value) =>
        values.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));

    private static bool IsOn(string output) =>
        output.Contains("on", StringComparison.OrdinalIgnoreCase) &&
        !output.Contains("off", StringComparison.OrdinalIgnoreCase);

    private void EnsureInstalled()
    {
        if (!IsInstalled)
        {
            throw new InvalidOperationException(
                "HidHide غير مثبت. ثبّته من https://github.com/nefarius/HidHide ثم أعد فتح التطبيق.");
        }
    }

    private async Task<CliResult> RunDirectAsync(string arguments, CancellationToken cancellationToken)
    {
        var cli = CliPath ?? throw new InvalidOperationException("HidHide غير مثبت.");

        try
        {
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

            return new CliResult(process.ExitCode == 0, (standardOutput + standardError).Trim());
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            AppLog.Warn($"HidHideCLI '{arguments}' failed: {ex.Message}");
            return new CliResult(false, string.Empty);
        }
    }

    /// <summary>
    /// Runs several HidHideCLI commands in one elevated cmd script. Each command writes its own
    /// stdout/stderr and exit code to a temporary file, so the results are known even though the
    /// elevated process window is hidden. Arguments live in the script file, which avoids the
    /// nesting and '&amp;' quoting problems of passing device instance ids through cmd /c.
    /// </summary>
    private async Task<IReadOnlyList<CliResult>> RunElevatedBatchAsync(
        IReadOnlyList<string> commands,
        CancellationToken cancellationToken)
    {
        var cli = CliPath ?? throw new InvalidOperationException("HidHide غير مثبت.");
        var directory = Path.Combine(Path.GetTempPath(), "DualSenseXboxBridge");
        Directory.CreateDirectory(directory);

        var id = Guid.NewGuid().ToString("N");
        var scriptPath = Path.Combine(directory, id + ".cmd");
        var outputs = commands.Select((_, index) => Path.Combine(directory, $"{id}.{index}.out")).ToList();
        var codes = commands.Select((_, index) => Path.Combine(directory, $"{id}.{index}.code")).ToList();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("@echo off");
        for (var i = 0; i < commands.Count; i++)
        {
            builder.AppendLine($"\"{cli}\" {commands[i]} > \"{outputs[i]}\" 2>&1");
            builder.AppendLine($"echo %ERRORLEVEL%> \"{codes[i]}\"");
        }

        try
        {
            await File.WriteAllTextAsync(scriptPath, builder.ToString(), cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo(scriptPath)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return Array.Empty<CliResult>();
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<CliResult>(commands.Count);
            for (var i = 0; i < commands.Count; i++)
            {
                var output = File.Exists(outputs[i])
                    ? (await File.ReadAllTextAsync(outputs[i], cancellationToken).ConfigureAwait(false)).Trim()
                    : string.Empty;

                var code = 1;
                if (File.Exists(codes[i]))
                {
                    var text = (await File.ReadAllTextAsync(codes[i], cancellationToken).ConfigureAwait(false)).Trim();
                    if (int.TryParse(text, out var parsed))
                    {
                        code = parsed;
                    }
                }

                results.Add(new CliResult(code == 0, output));
            }

            return results;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            LastError = "تم إلغاء طلب صلاحيات المسؤول.";
            AppLog.Warn(LastError);
            return Array.Empty<CliResult>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            AppLog.Warn("HidHide elevated batch failed: " + ex.Message);
            return Array.Empty<CliResult>();
        }
        finally
        {
            TryDelete(scriptPath);
            foreach (var path in outputs.Concat(codes))
            {
                TryDelete(path);
            }
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
        foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
        {
            string? directory = null;
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view);
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

    private readonly record struct CliResult(bool Success, string Output);
}
