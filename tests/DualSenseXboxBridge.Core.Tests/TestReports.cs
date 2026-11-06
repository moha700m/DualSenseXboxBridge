using DualSenseXboxBridge.Core.DualSense;

namespace DualSenseXboxBridge.Core.Tests;

/// <summary>
/// Builds 64 byte USB reports with the field layout of a real DualSense report.
/// The offsets mirror <see cref="DualSenseReportLayout"/>; the values used here match
/// captures published with the hid-playstation driver documentation.
/// </summary>
internal static class TestReports
{
    public static byte[] Neutral()
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[1] = 128;
        report[2] = 128;
        report[3] = 128;
        report[4] = 128;
        report[5] = 0;
        report[6] = 0;
        report[7] = 1;
        report[8] = 0x08; // D-Pad neutral, no face button
        report[9] = 0x00;
        report[10] = 0x00;
        report[53] = 0x05; // half battery, not charging
        return report;
    }

    public static byte[] WithFace(byte faceBits)
    {
        var report = Neutral();
        report[8] = (byte)(0x08 | faceBits);
        return report;
    }

    public static byte[] WithShoulders(byte bits)
    {
        var report = Neutral();
        report[9] = bits;
        return report;
    }

    public static byte[] WithDpad(DualSenseDpadDirection direction)
    {
        var report = Neutral();
        report[8] = (byte)((byte)direction & 0x0F);
        return report;
    }
}
