namespace DualSenseXboxBridge.Services;

/// <summary>
/// Finds a VIIPER server binary: the copy bundled with the installer first, then a copy next to
/// the app, in the user profile, or on PATH.
/// </summary>
public static class ViiperServerLocator
{
    public static string? FindBundled()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "thirdparty", "viiper", "viiper.exe"),
            Path.Combine(baseDirectory, "viiper.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string? Find()
    {
        var bundled = FindBundled();
        if (bundled is not null)
        {
            return bundled;
        }

        foreach (var candidate in LocalCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(directory.Trim(), "viiper.exe");
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch
            {
                // Malformed PATH entry: ignore.
            }
        }

        return null;
    }

    private static IEnumerable<string> LocalCandidates()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(localAppData, "VIIPER", "viiper.exe");
        yield return Path.Combine(localAppData, "Programs", "VIIPER", "viiper.exe");
    }
}
