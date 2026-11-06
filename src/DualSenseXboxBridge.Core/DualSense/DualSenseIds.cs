namespace DualSenseXboxBridge.Core.DualSense;

/// <summary>
/// Hardware identifiers of the Sony DualSense family and the USB report contract.
/// Values are verified against the public USB descriptors of the DualSense (CFI-ZCT1W)
/// and the DualSense Edge (CFI-ZCP1).
/// </summary>
public static class DualSenseIds
{
    /// <summary>Sony Interactive Entertainment vendor id.</summary>
    public const int SonyVendorId = 0x054C;

    /// <summary>DualSense Wireless Controller (all colour revisions).</summary>
    public const int DualSenseProductId = 0x0CE6;

    /// <summary>DualSense Edge Wireless Controller.</summary>
    public const int DualSenseEdgeProductId = 0x0DF2;

    /// <summary>The HID game-pad collection exposes a 64 byte input report over USB.</summary>
    public const int UsbInputReportLength = 64;

    public const byte UsbInputReportId = 0x01;

    /// <summary>Bluetooth uses report id 0x31 with a different layout; not supported yet.</summary>
    public const byte BluetoothInputReportId = 0x31;

    public static bool IsSupportedProductId(int productId) =>
        productId is DualSenseProductId or DualSenseEdgeProductId;

    public static string GetModelName(int productId) => productId switch
    {
        DualSenseProductId => "DualSense Wireless Controller",
        DualSenseEdgeProductId => "DualSense Edge Wireless Controller",
        _ => $"Unknown Sony device (PID 0x{productId:X4})"
    };
}
