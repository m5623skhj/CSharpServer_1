using CSharpServer.Packet;

namespace UnitTest.Packet
{
    public class PacketEncoderTest
    {
        [Fact]
        public void Encode_ReturnsLengthPrefixedPacket_WhenPayloadIsGiven()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var packet = PacketEncoder.Encode(payload);

            Assert.Equal(
                new byte[] { 0x05, 0x00, 0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F },
                packet);
        }

        [Fact]
        public void Encode_ReturnsHeaderOnlyPacket_WhenPayloadIsEmpty()
        {
            var packet = PacketEncoder.Encode([]);

            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, packet);
        }
    }
}