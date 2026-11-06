using System.Buffers.Binary;
using DualSenseXboxBridge.Core.Mapping;

namespace DualSenseXboxBridge.Core.Viiper;

/// <summary>
/// Wire format of the VIIPER Xbox 360 device stream: a 20 byte little endian input state
/// written to the server, and a 2 byte rumble feedback packet read from it.
/// See https://github.com/Alia5/VIIPER/blob/main/docs/devices/xbox360.md
/// </summary>
public static class ViiperXbox360Protocol
{
    public const int InputPacketSize = 20;
    public const int FeedbackPacketSize = 2;

    public static void WriteState(Span<byte> destination, in XboxPadState state)
    {
        if (destination.Length < InputPacketSize)
        {
            throw new ArgumentException($"Need {InputPacketSize} bytes.", nameof(destination));
        }

        destination.Slice(0, InputPacketSize).Clear();

        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)state.Buttons);
        destination[4] = state.LeftTrigger;
        destination[5] = state.RightTrigger;
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(6, 2), state.LeftStickX);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(8, 2), state.LeftStickY);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(10, 2), state.RightStickX);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(12, 2), state.RightStickY);
        // Bytes 14..19 are reserved by the device subtype.
    }

    public static (byte LeftMotor, byte RightMotor) ParseFeedback(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < FeedbackPacketSize)
        {
            throw new ArgumentException($"Need {FeedbackPacketSize} bytes.", nameof(packet));
        }

        return (packet[0], packet[1]);
    }
}
