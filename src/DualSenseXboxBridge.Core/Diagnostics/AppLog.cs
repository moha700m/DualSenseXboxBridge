using System.Text;

namespace DualSenseXboxBridge.Core.Diagnostics;

/// <summary>Minimal rolling file log so a user can attach diagnostics to a bug report.</summary>
public static class AppLog
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object Gate = new();

    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DualSenseXboxBridge",
        "logs");

    public static string CurrentFile => Path.Combine(Directory, "app.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : message + Environment.NewLine + exception);

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                System.IO.Directory.CreateDirectory(Directory);
                RollIfNeeded();
                var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(CurrentFile, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never take the app down.
        }
    }

    private static void RollIfNeeded()
    {
        var info = new FileInfo(CurrentFile);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        var previous = Path.Combine(Directory, "app.previous.log");
        if (File.Exists(previous))
        {
            File.Delete(previous);
        }

        File.Move(CurrentFile, previous);
    }
}
