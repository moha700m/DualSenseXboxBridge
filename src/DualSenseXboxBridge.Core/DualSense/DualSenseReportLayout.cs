namespace DualSenseXboxBridge.Core.DualSense;

/// <summary>
/// Byte offsets of the DualSense USB input report (report id 0x01, 64 bytes).
/// The layout is shared by the DualSense and the DualSense Edge; the Edge only adds
/// extra paddle buttons outside the range decoded here.
/// </summary>
public static class DualSenseReportLayout
{
    public const int ReportId = 0;
    public const int LeftStickX = 1;
    public const int LeftStickY = 2;
    public const int RightStickX = 3;
    public const int RightStickY = 4;
    public const int LeftTrigger = 5;
    public const int RightTrigger = 6;
    public const int SequenceNumber = 7;

    /// <summary>D-Pad in bits 0-3, Square/Cross/Circle/Triangle in bits 4-7.</summary>
    public const int FaceButtonsAndDpad = 8;

    /// <summary>L1/R1/L2/R2/Create/Options/L3/R3.</summary>
    public const int ShoulderAndSystemButtons = 9;

    /// <summary>PS/Touchpad/Mute.</summary>
    public const int SpecialButtons = 10;

    /// <summary>Battery level (0-10) in the low nibble, charging flag in bit 4.</summary>
    public const int Battery = 53;

    public const int MinimumLength = DualSenseIds.UsbInputReportLength;
}
