namespace DualSenseXboxBridge;

/// <summary>Parser for the standard 64-byte DualSense USB input report (ID 0x01).
/// Bluetooth uses a different packet format and is deliberately not accepted here.</summary>
public static class DualSenseReportParser
{
    private const byte UsbReportId = 0x01;

    public static bool TryParseUsb(ReadOnlySpan<byte> report, out XboxPadState state)
    {
        state = new XboxPadState(0, 0, 0, 0, 0, 0, 0);
        if (report.Length < 64 || report[0] != UsbReportId) return false;

        byte dpadAndFace = report[8];
        byte shouldersAndOptions = report[9];
        byte special = report[10];
        ushort buttons = 0;

        switch (dpadAndFace & 0x0F)
        {
            case 0: buttons |= 0x0001; break;
            case 1: buttons |= 0x0001 | 0x0008; break;
            case 2: buttons |= 0x0008; break;
            case 3: buttons |= 0x0002 | 0x0008; break;
            case 4: buttons |= 0x0002; break;
            case 5: buttons |= 0x0002 | 0x0004; break;
            case 6: buttons |= 0x0004; break;
            case 7: buttons |= 0x0001 | 0x0004; break;
        }

        if ((dpadAndFace & 0x10) != 0) buttons |= 0x4000;
        if ((dpadAndFace & 0x20) != 0) buttons |= 0x1000;
        if ((dpadAndFace & 0x40) != 0) buttons |= 0x2000;
        if ((dpadAndFace & 0x80) != 0) buttons |= 0x8000;
        if ((shouldersAndOptions & 0x01) != 0) buttons |= 0x0100;
        if ((shouldersAndOptions & 0x02) != 0) buttons |= 0x0200;
        if ((shouldersAndOptions & 0x10) != 0) buttons |= 0x0020;
        if ((shouldersAndOptions & 0x20) != 0) buttons |= 0x0010;
        if ((shouldersAndOptions & 0x40) != 0) buttons |= 0x0040;
        if ((shouldersAndOptions & 0x80) != 0) buttons |= 0x0080;
        if ((special & 0x01) != 0) buttons |= 0x0400;

        state = new XboxPadState(buttons, report[5], report[6], ToAxis(report[1], false), ToAxis(report[2], true), ToAxis(report[3], false), ToAxis(report[4], true));
        return true;
    }

    private static short ToAxis(byte raw, bool invert)
    {
        int centered = (raw - 128) * 258;
        if (invert) centered = -centered;
        return (short)Math.Clamp(centered, short.MinValue, short.MaxValue);
    }
}
