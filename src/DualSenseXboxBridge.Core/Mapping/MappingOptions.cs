namespace DualSenseXboxBridge.Core.Mapping;

/// <summary>User tunable input conversion settings. All values are validated on load.</summary>
public sealed class MappingOptions
{
    /// <summary>Radial stick dead zone, 0..0.9 of full travel.</summary>
    public double StickDeadzone { get; set; } = 0.08;

    /// <summary>Trigger dead zone, 0..0.9 of the 0..255 range.</summary>
    public double TriggerDeadzone { get; set; } = 0.02;

    /// <summary>Stick gain applied after the dead zone, 0.2..3.0.</summary>
    public double StickSensitivity { get; set; } = 1.0;

    /// <summary>Extra inversion on top of the standard conversion (which already flips the Y axis).</summary>
    public bool InvertLeftX { get; set; }
    public bool InvertLeftY { get; set; }
    public bool InvertRightX { get; set; }
    public bool InvertRightY { get; set; }

    public bool SwapSticks { get; set; }

    /// <summary>Optional per-button overrides. Key: DualSenseButtons name, value: Xbox360Buttons name.</summary>
    public Dictionary<string, string> ButtonOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Clamps every value into its supported range.</summary>
    public void Normalize()
    {
        StickDeadzone = Math.Clamp(StickDeadzone, 0.0, 0.9);
        TriggerDeadzone = Math.Clamp(TriggerDeadzone, 0.0, 0.9);
        StickSensitivity = Math.Clamp(StickSensitivity, 0.2, 3.0);
        ButtonOverrides ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public MappingOptions Clone()
    {
        var clone = (MappingOptions)MemberwiseClone();
        clone.ButtonOverrides = new Dictionary<string, string>(ButtonOverrides, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}
