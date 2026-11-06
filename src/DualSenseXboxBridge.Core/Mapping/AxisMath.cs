namespace DualSenseXboxBridge.Core.Mapping;

public static class AxisMath
{
    /// <summary>
    /// Applies a radial dead zone to a stick pair and rescales the remaining travel to the
    /// full -32768..32767 range so a game never sees the "dead" band as lost resolution.
    /// </summary>
    public static (short X, short Y) ApplyDeadzone(short x, short y, double deadzone, double sensitivity)
    {
        var magnitude = Math.Sqrt((double)x * x + (double)y * y) / 32767.0;
        if (magnitude <= 1e-4)
        {
            return (0, 0);
        }

        var zone = Math.Clamp(deadzone, 0.0, 0.95);
        var gain = Math.Clamp(sensitivity, 0.1, 4.0);

        if (magnitude <= zone)
        {
            return (0, 0);
        }

        var scaled = (magnitude - zone) / (1.0 - zone) * gain;
        scaled = Math.Clamp(scaled, 0.0, 1.0);

        var factor = scaled / magnitude;
        var nx = (short)Math.Clamp(Math.Round(x * factor), -32768.0, 32767.0);
        var ny = (short)Math.Clamp(Math.Round(y * factor), -32768.0, 32767.0);
        return (nx, ny);
    }

    /// <summary>
    /// Normalises a trigger value: values inside the dead zone become 0 and the rest is
    /// stretched back to 0..255 so the full pull still reaches 255.
    /// </summary>
    public static byte NormalizeTrigger(byte raw, double deadzone)
    {
        var zone = Math.Clamp(deadzone, 0.0, 0.95) * 255.0;
        if (raw <= zone)
        {
            return 0;
        }

        var scaled = (raw - zone) / (255.0 - zone) * 255.0;
        return (byte)Math.Clamp(Math.Round(scaled), 0.0, 255.0);
    }

    /// <summary>Converts a stick axis to a -100..100 percentage for the UI.</summary>
    public static double ToPercent(short value) => Math.Round(value / 32767.0 * 100.0, 1);
}
