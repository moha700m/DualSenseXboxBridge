using DualSenseXboxBridge.Core.HidHide;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class HidHideTests
{
    [Fact]
    public void DeviceInterfacePath_IsConvertedToInstanceId()
    {
        const string path = @"\\?\hid#vid_054c&pid_0ce6&mi_03#7&1a2b3c&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

        var id = HidHideDevicePath.ToDeviceInstanceId(path);

        Assert.Equal(@"HID\VID_054C&PID_0CE6&MI_03\7&1a2b3c&0&0000", id);
    }

    [Fact]
    public void DeviceInstancePathWithoutInterfaceGuid_IsAccepted()
    {
        const string path = @"\\?\hid#vid_054c&pid_0df2&mi_03#7&2&0000";

        Assert.True(HidHideDevicePath.TryToDeviceInstanceId(path, out var id));
        Assert.Equal(@"HID\VID_054C&PID_0DF2&MI_03\7&2&0000", id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-device-path")]
    public void InvalidPath_IsRejected(string path)
    {
        Assert.False(HidHideDevicePath.TryToDeviceInstanceId(path, out _));
    }

    [Fact]
    public void Diff_ComputesAdditionsAndRemovals()
    {
        var current = new[] { @"HID\VID_054C&PID_0CE6\1", @"HID\VID_1234&PID_5678\2" };
        var desired = new[] { @"HID\VID_054C&PID_0CE6\1", @"HID\VID_9999&PID_0001\3" };

        var diff = HidHideDiff.Compute(current, desired);

        Assert.Single(diff.ToAdd);
        Assert.Equal(@"HID\VID_9999&PID_0001\3", diff.ToAdd[0]);
        Assert.Single(diff.ToRemove);
        Assert.Equal(@"HID\VID_1234&PID_5678\2", diff.ToRemove[0]);
    }

    [Fact]
    public void Diff_IsCaseInsensitive()
    {
        var diff = HidHideDiff.Compute(
            new[] { @"hid\vid_054c&pid_0ce6\1" },
            new[] { @"HID\VID_054C&PID_0CE6\1" });

        Assert.Empty(diff.ToAdd);
        Assert.Empty(diff.ToRemove);
    }
}
