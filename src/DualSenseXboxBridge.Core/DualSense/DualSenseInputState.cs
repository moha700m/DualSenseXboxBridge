namespace DualSenseXboxBridge.Core.DualSense;

/// <summary>Decoded state of one USB input report.</summary>
public readonly record struct DualSenseInputState(
    DualSenseButtons Buttons,
    DualSenseDpadDirection Dpad,
    byte LeftStickX,
    byte LeftStickY,
    byte RightStickX,
    byte RightStickY,
    byte LeftTrigger,
    byte RightTrigger,
    byte SequenceNumber,
    int BatteryLevel,
    bool IsCharging)
{
    /// <summary>Battery level expressed as 0-100 percent, or -1 when the report carries no value.</summary>
    public int BatteryPercent => BatteryLevel < 0 ? -1 : Math.Clamp(BatteryLevel * 10, 0, 100);

    public bool IsPressed(DualSenseButtons button) => button != DualSenseButtons.None && (Buttons & button) == button;
}
