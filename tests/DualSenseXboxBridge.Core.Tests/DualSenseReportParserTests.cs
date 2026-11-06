using DualSenseXboxBridge.Core.DualSense;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class DualSenseReportParserTests
{
    [Fact]
    public void NeutralReport_ParsesWithoutButtons()
    {
        var result = DualSenseReportParser.ParseUsb(TestReports.Neutral());

        Assert.True(result.Success);
        Assert.Equal(DualSenseButtons.None, result.State.Buttons);
        Assert.Equal(DualSenseDpadDirection.Neutral, result.State.Dpad);
        Assert.Equal(0, result.State.LeftTrigger);
        Assert.Equal(128, result.State.LeftStickX);
    }

    [Theory]
    [InlineData(0x10, DualSenseButtons.Square)]
    [InlineData(0x20, DualSenseButtons.Cross)]
    [InlineData(0x40, DualSenseButtons.Circle)]
    [InlineData(0x80, DualSenseButtons.Triangle)]
    public void FaceButtons_AreDecoded(byte bits, DualSenseButtons expected)
    {
        var result = DualSenseReportParser.ParseUsb(TestReports.WithFace(bits));

        Assert.True(result.Success);
        Assert.Equal(expected, result.State.Buttons);
    }

    [Theory]
    [InlineData(0x01, DualSenseButtons.L1)]
    [InlineData(0x02, DualSenseButtons.R1)]
    [InlineData(0x04, DualSenseButtons.L2)]
    [InlineData(0x08, DualSenseButtons.R2)]
    [InlineData(0x10, DualSenseButtons.Create)]
    [InlineData(0x20, DualSenseButtons.Options)]
    [InlineData(0x40, DualSenseButtons.L3)]
    [InlineData(0x80, DualSenseButtons.R3)]
    public void ShoulderAndSystemButtons_AreDecoded(byte bits, DualSenseButtons expected)
    {
        var result = DualSenseReportParser.ParseUsb(TestReports.WithShoulders(bits));

        Assert.True(result.Success);
        Assert.Equal(expected, result.State.Buttons);
    }

    [Theory]
    [InlineData(0x01, DualSenseButtons.Ps)]
    [InlineData(0x02, DualSenseButtons.Touchpad)]
    [InlineData(0x04, DualSenseButtons.Mute)]
    public void SpecialButtons_AreDecoded(byte bits, DualSenseButtons expected)
    {
        var report = TestReports.Neutral();
        report[10] = bits;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.True(result.Success);
        Assert.Equal(expected, result.State.Buttons);
    }

    [Theory]
    [InlineData(DualSenseDpadDirection.Up)]
    [InlineData(DualSenseDpadDirection.UpRight)]
    [InlineData(DualSenseDpadDirection.Right)]
    [InlineData(DualSenseDpadDirection.DownRight)]
    [InlineData(DualSenseDpadDirection.Down)]
    [InlineData(DualSenseDpadDirection.DownLeft)]
    [InlineData(DualSenseDpadDirection.Left)]
    [InlineData(DualSenseDpadDirection.UpLeft)]
    public void Dpad_IsDecoded(DualSenseDpadDirection direction)
    {
        var result = DualSenseReportParser.ParseUsb(TestReports.WithDpad(direction));

        Assert.True(result.Success);
        Assert.Equal(direction, result.State.Dpad);
    }

    [Fact]
    public void OutOfRangeHatValue_FallsBackToNeutral()
    {
        var report = TestReports.Neutral();
        report[8] = 0x0F;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.True(result.Success);
        Assert.Equal(DualSenseDpadDirection.Neutral, result.State.Dpad);
    }

    [Fact]
    public void BluetoothReport_IsRejected()
    {
        var report = TestReports.Neutral();
        report[0] = 0x31;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.False(result.Success);
        Assert.Equal(DualSenseParseFailure.BluetoothReport, result.Failure);
    }

    [Fact]
    public void UnknownReportId_IsRejected()
    {
        var report = TestReports.Neutral();
        report[0] = 0x02;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.False(result.Success);
        Assert.Equal(DualSenseParseFailure.UnsupportedReportId, result.Failure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(63)]
    public void ShortReport_IsRejected(int length)
    {
        var report = new byte[length];

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.False(result.Success);
        Assert.Equal(DualSenseParseFailure.ReportTooShort, result.Failure);
    }

    [Fact]
    public void Triggers_AreRead()
    {
        var report = TestReports.Neutral();
        report[5] = 200;
        report[6] = 42;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.True(result.Success);
        Assert.Equal(200, result.State.LeftTrigger);
        Assert.Equal(42, result.State.RightTrigger);
    }

    [Fact]
    public void BatteryLevel_IsConvertedToPercent()
    {
        var report = TestReports.Neutral();
        report[53] = 0x0A;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.True(result.Success);
        Assert.Equal(10, result.State.BatteryLevel);
        Assert.Equal(100, result.State.BatteryPercent);
    }

    [Fact]
    public void BatteryLevel_AboveTen_IsClamped()
    {
        var report = TestReports.Neutral();
        report[53] = 0x0F;

        var result = DualSenseReportParser.ParseUsb(report);

        Assert.True(result.Success);
        Assert.Equal(10, result.State.BatteryLevel);
    }
}
