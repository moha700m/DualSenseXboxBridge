namespace DualSenseXboxBridge.Core.HidHide;

/// <summary>
/// Converts a Windows device interface path (as reported by HID enumeration, e.g.
/// \\?\hid#vid_054c&amp;pid_0ce6&amp;mi_03#7&amp;1a2b3c&amp;0&amp;0000#{guid})
/// into the device instance id HidHide expects (HID\VID_054C&amp;PID_0CE6&amp;MI_03\7&amp;1a2b3c&amp;0&amp;0000).
/// </summary>
public static class HidHideDevicePath
{
    public static bool TryToDeviceInstanceId(string? deviceInterfacePath, out string deviceInstanceId)
    {
        deviceInstanceId = string.Empty;
        if (string.IsNullOrWhiteSpace(deviceInterfacePath))
        {
            return false;
        }

        var path = deviceInterfacePath.Trim();
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            path = path[4..];
        }

        var interfaceSeparator = path.IndexOf("#{", StringComparison.Ordinal);
        if (interfaceSeparator >= 0)
        {
            path = path[..interfaceSeparator];
        }

        var segments = path.Split('#', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return false;
        }

        var parts = new List<string>(segments.Length);
        parts.Add(segments[0].ToUpperInvariant());
        parts.Add(NormalizeHardwareId(segments[1]));
        for (var i = 2; i < segments.Length; i++)
        {
            parts.Add(segments[i]);
        }

        deviceInstanceId = string.Join('\\', parts);
        return true;
    }

    public static string ToDeviceInstanceId(string deviceInterfacePath)
    {
        if (!TryToDeviceInstanceId(deviceInterfacePath, out var id))
        {
            throw new FormatException($"'{deviceInterfacePath}' is not a Windows device interface path.");
        }

        return id;
    }

    private static string NormalizeHardwareId(string segment)
    {
        var tokens = segment.Split('&');
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.Length > 4 &&
                (token.StartsWith("vid_", StringComparison.OrdinalIgnoreCase) ||
                 token.StartsWith("pid_", StringComparison.OrdinalIgnoreCase) ||
                 token.StartsWith("mi_", StringComparison.OrdinalIgnoreCase)))
            {
                tokens[i] = token[..4].ToUpperInvariant() + token[4..].ToUpperInvariant();
            }
        }

        return string.Join('&', tokens);
    }
}
