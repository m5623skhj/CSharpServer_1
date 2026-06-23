using CSharpServer.Packet;

namespace UnitTest.Packet
{
    public class PacketCodecTest
    {
        [Fact]
        public void EncodeAndReadPacket_ReturnsOriginalPayload()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var encodedPacket = PacketEncoder.Encode(payload);

            var buffer = new PacketBuffer();
            buffer.Append(encodedPacket);

            var result = buffer.TryReadPacket(out var decodedPayload);

            Assert.True(result);
            Assert.Equal(payload, decodedPayload);
        }
    }
}