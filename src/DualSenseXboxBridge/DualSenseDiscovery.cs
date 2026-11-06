using HidSharp;

namespace DualSenseXboxBridge;

public sealed class DualSenseDiscovery
{
    private const int SonyVendorId = 0x054C;
    private static readonly HashSet<int> KnownProductIds = new() { 0x0CE6, 0x0DF2 };

    public List<HidDevice> FindDualSense()
    {
        return DeviceList.Local.GetHidDevices(SonyVendorId)
            .Where(d => KnownProductIds.Contains(d.ProductID) && d.MaxInputReportLength >= 64)
            .ToList();
    }
}
