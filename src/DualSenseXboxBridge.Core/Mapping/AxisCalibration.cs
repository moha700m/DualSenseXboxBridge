namespace DualSenseXboxBridge.Core.Mapping;

/// <summary>
/// Per-axis calibration of a raw 8 bit stick axis. The default (128/0/255) matches the
/// factory centre and the full electrical range of a DualSense; a worn stick can drift,
/// so the UI exposes the centre offset as a setting.
/// </summary>
public readonly record struct AxisCalibration(byte Center, byte Minimum, byte Maximum)
{
    public static AxisCalibration Default { get; } = new(128, 0, 255);

    public bool IsDefault => Center == 128 && Minimum == 0 && Maximum == 255;

    public short ToAxis(byte raw, bool invert)
    {
        var span = raw >= Center ? Maximum - Center : Center - Minimum;
        if (span <= 0)
        {
            span = 1;
        }

        var normalized = (raw - (double)Center) / span;
        normalized = Math.Clamp(normalized, -1.0, 1.0);

        if (invert)
        {
            normalized = -normalized;
        }

        var value = Math.Round(normalized * 32767.0, MidpointRounding.AwayFromZero);
        return (short)Math.Clamp(value, -32768.0, 32767.0);
    }

    /// <summary>Creates a calibration with a shifted centre, keeping the ends intact.</summary>
    public static AxisCalibration WithCenter(int center) =>
        new((byte)Math.Clamp(center, 16, 240), 0, 255);
}
