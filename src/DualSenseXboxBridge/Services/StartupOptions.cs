namespace DualSenseXboxBridge.Services;

/// <summary>Command line options understood by the app.</summary>
public sealed class StartupOptions
{
    public bool Minimized { get; init; }

    public static StartupOptions Parse(string[] args)
    {
        var minimized = args.Any(a =>
            string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "/minimized", StringComparison.OrdinalIgnoreCase));

        return new StartupOptions { Minimized = minimized };
    }
}
