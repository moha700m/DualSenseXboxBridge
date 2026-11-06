namespace DualSenseXboxBridge.Core.DualSense;

/// <summary>
/// Hat switch value decoded from bits 0-3 of report byte 8.
/// The DualSense reports 0 (up) through 7 (up-left), and 8 for "released".
/// </summary>
public enum DualSenseDpadDirection
{
    Up = 0,
    UpRight = 1,
    Right = 2,
    DownRight = 3,
    Down = 4,
    DownLeft = 5,
    Left = 6,
    UpLeft = 7,
    Neutral = 8
}
