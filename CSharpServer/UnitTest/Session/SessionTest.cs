using CSharpServer.Packet;
using NetworkSession = CSharpServer.Network.Session;

namespace UnitTest.Session
{
    public class SessionTest
    {
        [Fact]
        public void Receive_InvokesPacketHandler_WhenCompletePacketIsReceived()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));

            session.Receive(PacketEncoder.Encode(payload));

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Receive_DoesNotInvokePacketHandler_UntilPacketIsComplete()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));

            session.Receive([0x05, 0x00]);

            Assert.Empty(receivedPackets);

            session.Receive([0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F]);

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Receive_InvokesPacketHandlerInOrder_WhenMultiplePacketsAreReceived()
        {
            var firstPayload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var secondPayload = new byte[] { 0x77, 0x6F, 0x72, 0x6C, 0x64 };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));
            var receivedData = PacketEncoder.Encode(firstPayload)
                .Concat(PacketEncoder.Encode(secondPayload))
                .ToArray();

            session.Receive(receivedData);

            Assert.Collection(
                receivedPackets,
                packet => Assert.Equal(firstPayload, packet),
                packet => Assert.Equal(secondPayload, packet));
        }
    }
}
