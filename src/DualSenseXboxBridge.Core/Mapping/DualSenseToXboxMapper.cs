using DualSenseXboxBridge.Core.DualSense;

namespace DualSenseXboxBridge.Core.Mapping;

/// <summary>Pure conversion from a decoded DualSense report to an Xbox 360 state.</summary>
public static class DualSenseToXboxMapper
{
    private static readonly Dictionary<DualSenseButtons, Xbox360Buttons> DefaultMap = new()
    {
        [DualSenseButtons.Cross] = Xbox360Buttons.A,
        [DualSenseButtons.Circle] = Xbox360Buttons.B,
        [DualSenseButtons.Square] = Xbox360Buttons.X,
        [DualSenseButtons.Triangle] = Xbox360Buttons.Y,
        [DualSenseButtons.L1] = Xbox360Buttons.LeftShoulder,
        [DualSenseButtons.R1] = Xbox360Buttons.RightShoulder,
        [DualSenseButtons.L2] = Xbox360Buttons.None, // analog only - see LeftTrigger
        [DualSenseButtons.R2] = Xbox360Buttons.None,
        [DualSenseButtons.Create] = Xbox360Buttons.Back,
        [DualSenseButtons.Options] = Xbox360Buttons.Start,
        [DualSenseButtons.L3] = Xbox360Buttons.LeftThumb,
        [DualSenseButtons.R3] = Xbox360Buttons.RightThumb,
        [DualSenseButtons.Ps] = Xbox360Buttons.Guide,
        [DualSenseButtons.Touchpad] = Xbox360Buttons.None,
        [DualSenseButtons.Mute] = Xbox360Buttons.None
    };

    public static XboxPadState Map(
        in DualSenseInputState input,
        MappingOptions? options = null,
        AxisCalibration? leftCalibration = null,
        AxisCalibration? rightCalibration = null)
    {
        options ??= new MappingOptions();
        var left = leftCalibration ?? AxisCalibration.Default;
        var right = rightCalibration ?? AxisCalibration.Default;

        var buttons = MapButtons(input.Buttons, options.ButtonOverrides);
        buttons |= MapDpad(input.Dpad);

        // The DualSense reports Y with 0 = up; XInput uses +32767 = up, so Y is always flipped.
        var lx = left.ToAxis(input.LeftStickX, options.InvertLeftX);
        var ly = left.ToAxis(input.LeftStickY, !options.InvertLeftY);
        var rx = right.ToAxis(input.RightStickX, options.InvertRightX);
        var ry = right.ToAxis(input.RightStickY, !options.InvertRightY);

        if (options.SwapSticks)
        {
            (lx, rx) = (rx, lx);
            (ly, ry) = (ry, ly);
        }

        var (leftX, leftY) = AxisMath.ApplyDeadzone(lx, ly, options.StickDeadzone, options.StickSensitivity);
        var (rightX, rightY) = AxisMath.ApplyDeadzone(rx, ry, options.StickDeadzone, options.StickSensitivity);

        var leftTrigger = AxisMath.NormalizeTrigger(input.LeftTrigger, options.TriggerDeadzone);
        var rightTrigger = AxisMath.NormalizeTrigger(input.RightTrigger, options.TriggerDeadzone);

        return new XboxPadState(buttons, leftTrigger, rightTrigger, leftX, leftY, rightX, rightY);
    }

    public static Xbox360Buttons MapButtons(DualSenseButtons pressed, IReadOnlyDictionary<string, string>? overrides = null)
    {
        var result = Xbox360Buttons.None;
        foreach (var button in Enum.GetValues<DualSenseButtons>())
        {
            if (button == DualSenseButtons.None || (pressed & button) != button)
            {
                continue;
            }

            result |= Resolve(button, overrides);
        }

        return result;
    }

    public static Xbox360Buttons Resolve(DualSenseButtons button, IReadOnlyDictionary<string, string>? overrides = null)
    {
        if (overrides is not null &&
            overrides.Count > 0 &&
            overrides.TryGetValue(button.ToString(), out var text) &&
            Enum.TryParse<Xbox360Buttons>(text, ignoreCase: true, out var mapped))
        {
            return mapped;
        }

        return DefaultMap.TryGetValue(button, out var value) ? value : Xbox360Buttons.None;
    }

    public static Xbox360Buttons MapDpad(DualSenseDpadDirection direction) => direction switch
    {
        DualSenseDpadDirection.Up => Xbox360Buttons.DpadUp,
        DualSenseDpadDirection.UpRight => Xbox360Buttons.DpadUp | Xbox360Buttons.DpadRight,
        DualSenseDpadDirection.Right => Xbox360Buttons.DpadRight,
        DualSenseDpadDirection.DownRight => Xbox360Buttons.DpadDown | Xbox360Buttons.DpadRight,
        DualSenseDpadDirection.Down => Xbox360Buttons.DpadDown,
        DualSenseDpadDirection.DownLeft => Xbox360Buttons.DpadDown | Xbox360Buttons.DpadLeft,
        DualSenseDpadDirection.Left => Xbox360Buttons.DpadLeft,
        DualSenseDpadDirection.UpLeft => Xbox360Buttons.DpadUp | Xbox360Buttons.DpadLeft,
        _ => Xbox360Buttons.None
    };
}
