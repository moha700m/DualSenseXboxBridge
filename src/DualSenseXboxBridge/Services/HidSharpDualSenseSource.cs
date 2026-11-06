using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.DualSense;
using DualSenseXboxBridge.Core.Hid;
using HidSharp;

namespace DualSenseXboxBridge.Services;

/// <summary>Enumerates Sony HID collections and opens the DualSense game-pad interface.</summary>
public sealed class HidSharpDualSenseSource
{
    public IReadOnlyList<HidDeviceDescriptor> Enumerate()
    {
        var results = new List<HidDeviceDescriptor>();

        try
        {
            foreach (var device in DeviceList.Local.GetHidDevices(DualSenseIds.SonyVendorId))
            {
                try
                {
                    results.Add(ToDescriptor(device));
                }
                catch (Exception ex)
                {
                    AppLog.Warn($"Skipping HID device '{SafePath(device)}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("HID enumeration failed: " + ex.Message);
        }

        return results;
    }

    public HidDeviceDescriptor? FindPreferred(string? preferredDevicePath) =>
        DualSenseDeviceFilter.Select(Enumerate(), preferredDevicePath);

    public HidInputHandle Open(HidDeviceDescriptor descriptor)
    {
        HidDevice? match = null;
        try
        {
            match = DeviceList.Local
                .GetHidDevices(DualSenseIds.SonyVendorId)
                .FirstOrDefault(d => string.Equals(SafePath(d), descriptor.DevicePath, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            AppLog.Warn("HID lookup failed: " + ex.Message);
        }

        if (match is null)
        {
            throw new InvalidOperationException("اليد لم تعد متصلة. أعد توصيل كابل USB ثم اضغط تحديث.");
        }

        if (!match.TryOpen(out var stream))
        {
            throw new IOException("تعذر فتح واجهة HID. أغلق أي برنامج آخر يستخدم اليد (Steam Input، DS4Windows…) ثم أعد المحاولة.");
        }

        try
        {
            stream.ReadTimeout = 250;
            return new HidInputHandle(stream, match.GetMaxInputReportLength());
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static HidDeviceDescriptor ToDescriptor(HidDevice device)
    {
        var path = SafePath(device);
        // ProductName/Manufacturer/SerialNumber are marked obsolete in HidSharp 2.6 without a
        // documented replacement; GetFriendlyName is the supported equivalent for the name.
        var productName = Try(() => device.GetFriendlyName());
        const string manufacturer = "Sony Interactive Entertainment";
        string? serial = null;
        var inputLength = SafeInt(() => device.GetMaxInputReportLength());
        var outputLength = SafeInt(() => device.GetMaxOutputReportLength());

        var isBluetooth = path.Contains("bth", StringComparison.OrdinalIgnoreCase);

        return new HidDeviceDescriptor(
            path,
            device.VendorID,
            device.ProductID,
            productName,
            manufacturer,
            serial,
            inputLength,
            outputLength,
            isBluetooth);
    }

    private static string SafePath(HidDevice device)
    {
        try
        {
            return device.DevicePath ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? Try(Func<string?> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return null;
        }
    }

    private static int SafeInt(Func<int> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>Owns an open HID stream and turns a read timeout into a zero-length result.</summary>
public sealed class HidInputHandle : IDisposable
{
    private readonly HidStream _stream;
    private bool _disposed;

    public HidInputHandle(HidStream stream, int reportLength)
    {
        _stream = stream;
        ReportLength = reportLength;
    }

    public int ReportLength { get; }

    public int Read(byte[] buffer, int timeoutMilliseconds)
    {
        if (_disposed)
        {
            return 0;
        }

        _stream.ReadTimeout = timeoutMilliseconds;
        try
        {
            return _stream.Read(buffer, 0, buffer.Length);
        }
        catch (TimeoutException)
        {
            return 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _stream.Dispose();
        }
        catch (Exception ex)
        {
            AppLog.Warn("HID stream dispose failed: " + ex.Message);
        }
    }
}
