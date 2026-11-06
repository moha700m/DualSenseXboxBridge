using DualSenseXboxBridge.Core.Hid;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class DeviceFilterTests
{
    private static HidDeviceDescriptor Device(
        int vendor = 0x054C,
        int product = 0x0CE6,
        int reportLength = 64,
        bool bluetooth = false,
        string path = @"\\?\hid#vid_054c&pid_0ce6&mi_03#7&1&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}") =>
        new(path, vendor, product, "Wireless Controller", "Sony", null, reportLength, 64, bluetooth);

    [Fact]
    public void DualSenseUsbInterface_IsAccepted()
    {
        Assert.True(DualSenseDeviceFilter.IsCandidate(Device(), out _));
    }

    [Fact]
    public void DualSenseEdge_IsAccepted()
    {
        Assert.True(DualSenseDeviceFilter.IsCandidate(Device(product: 0x0DF2), out _));
    }

    [Fact]
    public void DualShock4_IsRejected()
    {
        Assert.False(DualSenseDeviceFilter.IsCandidate(Device(product: 0x09CC), out var reason));
        Assert.Contains("0x09CC", reason);
    }

    [Fact]
    public void NonSonyDevice_IsRejected()
    {
        Assert.False(DualSenseDeviceFilter.IsCandidate(Device(vendor: 0x045E), out _));
    }

    [Fact]
    public void ShortReportInterface_IsRejected()
    {
        Assert.False(DualSenseDeviceFilter.IsCandidate(Device(reportLength: 32), out _));
    }

    [Fact]
    public void BluetoothDevice_IsRejectedForNow()
    {
        Assert.False(DualSenseDeviceFilter.IsCandidate(Device(bluetooth: true), out var reason));
        Assert.Contains("Bluetooth", reason);
    }

    [Fact]
    public void Select_PrefersStoredDevicePath()
    {
        var first = Device(path: @"\\?\hid#vid_054c&pid_0ce6&mi_03#a&0000#{guid}");
        var second = Device(path: @"\\?\hid#vid_054c&pid_0ce6&mi_03#b&0000#{guid}");

        var selected = DualSenseDeviceFilter.Select(new[] { first, second }, second.DevicePath);

        Assert.NotNull(selected);
        Assert.Equal(second.DevicePath, selected!.DevicePath);
    }

    [Fact]
    public void Select_FallsBackWhenPreferredIsGone()
    {
        var device = Device();

        var selected = DualSenseDeviceFilter.Select(new[] { device }, @"\\?\hid#gone");

        Assert.NotNull(selected);
        Assert.Equal(device.DevicePath, selected!.DevicePath);
    }

    [Fact]
    public void Select_ReturnsNullWhenNothingMatches()
    {
        Assert.Null(DualSenseDeviceFilter.Select(new[] { Device(vendor: 0x045E) }));
    }
}
