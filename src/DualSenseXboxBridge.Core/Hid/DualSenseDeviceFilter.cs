using DualSenseXboxBridge.Core.DualSense;

namespace DualSenseXboxBridge.Core.Hid;

/// <summary>
/// Decides which HID collections belong to the DualSense game-pad interface.
/// A DualSense exposes several collections (audio, microphone, sensors); only the one with
/// the 64 byte input report carries the controller state, so the filter rejects the rest.
/// </summary>
public static class DualSenseDeviceFilter
{
    public static bool IsCandidate(HidDeviceDescriptor device, out string reason)
    {
        if (device.VendorId != DualSenseIds.SonyVendorId)
        {
            reason = "Not a Sony device.";
            return false;
        }

        if (!DualSenseIds.IsSupportedProductId(device.ProductId))
        {
            reason = $"Unsupported Sony product id 0x{device.ProductId:X4}.";
            return false;
        }

        if (device.MaxInputReportLength < DualSenseIds.UsbInputReportLength)
        {
            reason = $"Input report is {device.MaxInputReportLength} bytes; the controller interface reports 64.";
            return false;
        }

        if (device.IsBluetooth)
        {
            reason = "Bluetooth transport is not supported in this release.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Picks the interface to use: the preferred device path when it is still present,
    /// otherwise the first candidate ordered by device path for a stable choice.
    /// </summary>
    public static HidDeviceDescriptor? Select(IEnumerable<HidDeviceDescriptor> devices, string? preferredDevicePath = null)
    {
        var candidates = devices
            .Where(d => IsCandidate(d, out _))
            .OrderBy(d => d.DevicePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferredDevicePath))
        {
            var preferred = candidates.FirstOrDefault(
                d => string.Equals(d.DevicePath, preferredDevicePath, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return candidates[0];
    }
}
