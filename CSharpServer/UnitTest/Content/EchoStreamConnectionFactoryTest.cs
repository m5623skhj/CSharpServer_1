using CSharpServer.Content;
using CSharpServer.Packet;

namespace UnitTest.Content
{
    public class EchoStreamConnectionFactoryTest
    {
        [Fact]
        public void Create_ReturnsConnectionThatWritesEchoPacketToStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var encodedPacket = PacketEncoder.Encode(payload);
            using var stream = new MemoryStream();
            stream.Write(encodedPacket);
            stream.Position = 0;

            var connection = EchoStreamConnectionFactory.Create(stream, inBufferSize: 16);

            connection.ReadOnce();

            Assert.Equal(encodedPacket.Concat(encodedPacket), stream.ToArray());
        }
    }
}
