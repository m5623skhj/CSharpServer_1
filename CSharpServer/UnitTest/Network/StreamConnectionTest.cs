using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class StreamConnectionTest
    {
        [Fact]
        public void ReadOnce_InvokesPacketHandler_WhenPacketIsReadFromStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream(PacketEncoder.Encode(payload));
            var receivedPackets = new List<byte[]>();
            var connection = new StreamConnection(stream, inBufferSize: 16, receivedPackets.Add);

            var result = connection.ReadOnce();

            Assert.True(result);
            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Send_WritesEncodedPacketToStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            connection.Send(payload);

            Assert.Equal(PacketEncoder.Encode(payload), stream.ToArray());
        }
    }
}
