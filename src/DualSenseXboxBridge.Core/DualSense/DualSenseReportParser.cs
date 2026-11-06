namespace DualSenseXboxBridge.Core.DualSense;

/// <summary>
/// Parses the 64 byte USB input report (id 0x01) of a DualSense.
/// Bluetooth reports (id 0x31, variable length) are rejected on purpose: they use a
/// different layout and are not supported in this release.
/// </summary>
public static class DualSenseReportParser
{
    public static DualSenseParseResult ParseUsb(ReadOnlySpan<byte> report)
    {
        if (report.Length < DualSenseReportLayout.MinimumLength)
        {
            return DualSenseParseResult.Fail(
                DualSenseParseFailure.ReportTooShort,
                $"USB input report must be {DualSenseReportLayout.MinimumLength} bytes, got {report.Length}.");
        }

        var reportId = report[DualSenseReportLayout.ReportId];
        if (reportId == DualSenseIds.BluetoothInputReportId)
        {
            return DualSenseParseResult.Fail(
                DualSenseParseFailure.BluetoothReport,
                "Bluetooth report (0x31) received. Bluetooth input is not supported in this build.");
        }

        if (reportId != DualSenseIds.UsbInputReportId)
        {
            return DualSenseParseResult.Fail(
                DualSenseParseFailure.UnsupportedReportId,
                $"Unexpected report id 0x{reportId:X2}; expected 0x{DualSenseIds.UsbInputReportId:X2}.");
        }

        var face = report[DualSenseReportLayout.FaceButtonsAndDpad];
        var shoulders = report[DualSenseReportLayout.ShoulderAndSystemButtons];
        var special = report[DualSenseReportLayout.SpecialButtons];

        var buttons = DualSenseButtons.None;

        if ((face & 0x10) != 0) buttons |= DualSenseButtons.Square;
        if ((face & 0x20) != 0) buttons |= DualSenseButtons.Cross;
        if ((face & 0x40) != 0) buttons |= DualSenseButtons.Circle;
        if ((face & 0x80) != 0) buttons |= DualSenseButtons.Triangle;

        if ((shoulders & 0x01) != 0) buttons |= DualSenseButtons.L1;
        if ((shoulders & 0x02) != 0) buttons |= DualSenseButtons.R1;
        if ((shoulders & 0x04) != 0) buttons |= DualSenseButtons.L2;
        if ((shoulders & 0x08) != 0) buttons |= DualSenseButtons.R2;
        if ((shoulders & 0x10) != 0) buttons |= DualSenseButtons.Create;
        if ((shoulders & 0x20) != 0) buttons |= DualSenseButtons.Options;
        if ((shoulders & 0x40) != 0) buttons |= DualSenseButtons.L3;
        if ((shoulders & 0x80) != 0) buttons |= DualSenseButtons.R3;

        if ((special & 0x01) != 0) buttons |= DualSenseButtons.Ps;
        if ((special & 0x02) != 0) buttons |= DualSenseButtons.Touchpad;
        if ((special & 0x04) != 0) buttons |= DualSenseButtons.Mute;

        var dpad = (DualSenseDpadDirection)(face & 0x0F);
        if ((int)dpad > (int)DualSenseDpadDirection.Neutral)
        {
            dpad = DualSenseDpadDirection.Neutral;
        }

        var batteryRaw = report[DualSenseReportLayout.Battery];
        var batteryLevel = batteryRaw & 0x0F;
        if (batteryLevel > 10) batteryLevel = 10;
        var charging = (batteryRaw & 0x10) != 0;

        var state = new DualSenseInputState(
            buttons,
            dpad,
            report[DualSenseReportLayout.LeftStickX],
            report[DualSenseReportLayout.LeftStickY],
            report[DualSenseReportLayout.RightStickX],
            report[DualSenseReportLayout.RightStickY],
            report[DualSenseReportLayout.LeftTrigger],
            report[DualSenseReportLayout.RightTrigger],
            report[DualSenseReportLayout.SequenceNumber],
            batteryLevel,
            charging);

        return DualSenseParseResult.Ok(state);
    }

    /// <summary>Validates a report without decoding it.</summary>
    public static bool LooksLikeDualSenseUsbReport(ReadOnlySpan<byte> report) =>
        report.Length >= DualSenseReportLayout.MinimumLength &&
        report[DualSenseReportLayout.ReportId] == DualSenseIds.UsbInputReportId;
}
