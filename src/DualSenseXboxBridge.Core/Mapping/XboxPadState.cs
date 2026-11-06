namespace DualSenseXboxBridge.Core.Mapping;

/// <summary>One Xbox 360 (XInput) state: buttons, both triggers and both sticks.</summary>
public readonly record struct XboxPadState(
    Xbox360Buttons Buttons,
    byte LeftTrigger,
    byte RightTrigger,
    short LeftStickX,
    short LeftStickY,
    short RightStickX,
    short RightStickY)
{
    /// <summary>All buttons released, triggers at rest and both sticks centred.</summary>
    public static XboxPadState Neutral => default;

    public bool IsNeutral =>
        Buttons == Xbox360Buttons.None &&
        LeftTrigger == 0 &&
        RightTrigger == 0 &&
        LeftStickX == 0 &&
        LeftStickY == 0 &&
        RightStickX == 0 &&
        RightStickY == 0;

    public bool IsPressed(Xbox360Buttons button) => button != Xbox360Buttons.None && (Buttons & button) == button;
}
