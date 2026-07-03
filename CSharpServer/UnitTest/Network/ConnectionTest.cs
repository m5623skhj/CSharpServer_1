using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class ConnectionTest
    {
        [Fact]
        public void ReceiveFromTransport_InvokesPacketHandler_WhenCompletePacketIsReceived()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, packet => receivedPackets.Add(packet));

            connection.ReceiveFromTransport(PacketEncoder.Encode(payload));

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Send_WritesEncodedPacketToTransport()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, _ => { });

            connection.Send(payload);

            var sentPacket = Assert.Single(transport.SentPackets);
            Assert.Equal(PacketEncoder.Encode(payload), sentPacket);
        }

        private sealed class FakeConnectionTransport : IConnectionTransport
        {
            public List<byte[]> SentPackets { get; } = [];

            public void Send(byte[] data)
            {
                SentPackets.Add(data);
            }
        }
    }
}
