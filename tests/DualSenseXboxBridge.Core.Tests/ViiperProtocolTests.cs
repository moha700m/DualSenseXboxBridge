using System.Buffers.Binary;
using DualSenseXboxBridge.Core.Mapping;
using DualSenseXboxBridge.Core.Viiper;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class ViiperProtocolTests
{
    [Fact]
    public void NeutralState_ProducesZeroedPacket()
    {
        Span<byte> packet = stackalloc byte[ViiperXbox360Protocol.InputPacketSize];
        packet.Fill(0xAA);

        ViiperXbox360Protocol.WriteState(packet, XboxPadState.Neutral);

        Assert.All(packet.ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void Packet_UsesLittleEndianLayout()
    {
        var state = new XboxPadState(
            Xbox360Buttons.A | Xbox360Buttons.Start,
            LeftTrigger: 200,
            RightTrigger: 100,
            LeftStickX: -32768,
            LeftStickY: 32767,
            RightStickX: 0,
            RightStickY: -1);

        var packet = new byte[ViiperXbox360Protocol.InputPacketSize];
        ViiperXbox360Protocol.WriteState(packet, state);

        Assert.Equal(20, packet.Length);
        Assert.Equal((uint)(Xbox360Buttons.A | Xbox360Buttons.Start), BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(0, 4)));
        Assert.Equal(200, packet[4]);
        Assert.Equal(100, packet[5]);
        Assert.Equal(-32768, BinaryPrimitives.ReadInt16LittleEndian(packet.AsSpan(6, 2)));
        Assert.Equal(32767, BinaryPrimitives.ReadInt16LittleEndian(packet.AsSpan(8, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(packet.AsSpan(10, 2)));
        Assert.Equal(-1, BinaryPrimitives.ReadInt16LittleEndian(packet.AsSpan(12, 2)));
        Assert.All(packet[14..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void WriteState_RejectsSmallBuffer()
    {
        var packet = new byte[19];

        Assert.Throws<ArgumentException>(() =>
        {
            ViiperXbox360Protocol.WriteState(packet, XboxPadState.Neutral);
        });
    }

    [Fact]
    public void Feedback_IsDecoded()
    {
        var feedback = ViiperXbox360Protocol.ParseFeedback(new byte[] { 10, 240 });

        Assert.Equal(10, feedback.LeftMotor);
        Assert.Equal(240, feedback.RightMotor);
    }
}
