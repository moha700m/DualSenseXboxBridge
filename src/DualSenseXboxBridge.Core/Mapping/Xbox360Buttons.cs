namespace DualSenseXboxBridge.Core.Mapping;

/// <summary>
/// Button flags of an Xbox 360 (XInput) report. The values are identical to the
/// XUSB / XInput button masks, so they can be forwarded to any virtual pad backend
/// without translation.
/// </summary>
[Flags]
public enum Xbox360Buttons : ushort
{
    None = 0,
    DpadUp = 0x0001,
    DpadDown = 0x0002,
    DpadLeft = 0x0004,
    DpadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    Guide = 0x0400,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}
