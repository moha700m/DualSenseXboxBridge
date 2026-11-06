namespace DualSenseXboxBridge.Core.Hid;

/// <summary>Transport neutral description of a HID collection found on the machine.</summary>
public sealed record HidDeviceDescriptor(
    string DevicePath,
    int VendorId,
    int ProductId,
    string? ProductName,
    string? Manufacturer,
    string? SerialNumber,
    int MaxInputReportLength,
    int MaxOutputReportLength,
    bool IsBluetooth)
{
    public string DisplayName => string.IsNullOrWhiteSpace(ProductName) ? $"HID device 0x{VendorId:X4}:0x{ProductId:X4}" : ProductName!;
}
