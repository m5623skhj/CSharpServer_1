using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class ConnectionTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenTransportIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Connection(null!, _ => { }));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPacketHandlerIsNull()
        {
            var transport = new FakeConnectionTransport();

            Assert.Throws<ArgumentNullException>(() =>
                new Connection(transport, (Action<byte[]>)null!));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPublicPacketHandlerIsNull()
        {
            var transport = new FakeConnectionTransport();

            var exception = Assert.Throws<ArgumentNullException>(() =>
                new Connection(transport, (IConnectionPacketHandler)null!));

            Assert.Equal("packetHandler", exception.ParamName);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenAsyncPacketHandlerIsNull()
        {
            var transport = new FakeConnectionTransport();

            Assert.Throws<ArgumentNullException>(() =>
                new Connection(transport, _ => { }, null!));
        }

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
        public async Task ReceiveFromTransportAsync_InvokesHandlerWithPayloadAndCancellationToken()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var transport = new FakeConnectionTransport();
            using var cancellation = new CancellationTokenSource();
            byte[]? receivedPayload = null;
            var receivedCancellationToken = CancellationToken.None;
            var connection = new Connection(
                transport,
                _ => { },
                (packet, cancellationToken) =>
                {
                    receivedPayload = packet;
                    receivedCancellationToken = cancellationToken;
                    return ValueTask.CompletedTask;
                });

            await connection.ReceiveFromTransportAsync(
                PacketEncoder.Encode(payload),
                cancellation.Token);

            Assert.Equal(payload, receivedPayload);
            Assert.Equal(cancellation.Token, receivedCancellationToken);
        }

        [Fact]
        public async Task ReceiveFromTransportAsync_PropagatesHandlerException()
        {
            var expectedException = new InvalidOperationException("Handler failed.");
            var transport = new FakeConnectionTransport();
            var connection = new Connection(
                transport,
                _ => { },
                (_, _) => ValueTask.FromException(expectedException));

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                connection.ReceiveFromTransportAsync(
                    PacketEncoder.Encode([0x01]),
                    CancellationToken.None).AsTask());

            Assert.Same(expectedException, actualException);
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

        [Fact]
        public async Task SendAsync_WritesEncodedPacketToTransport()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, _ => { });

            await connection.SendAsync(payload, CancellationToken.None);

            var sentPacket = Assert.Single(transport.SentPackets);
            Assert.Equal(PacketEncoder.Encode(payload), sentPacket);
        }

        [Fact]
        public void Close_ClosesTransport()
        {
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, _ => { });

            connection.Close();

            Assert.True(transport.IsClosed);
        }

        [Fact]
        public void ReceiveFromTransport_ThrowsObjectDisposedException_WhenConnectionWasClosed()
        {
            var receivedPackets = new List<byte[]>();
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, receivedPackets.Add);
            connection.Close();

            Assert.Throws<ObjectDisposedException>(() =>
                connection.ReceiveFromTransport(PacketEncoder.Encode([0x01])));
            Assert.Empty(receivedPackets);
        }

        [Fact]
        public async Task ReceiveFromTransportAsync_ThrowsObjectDisposedException_WhenConnectionWasClosed()
        {
            var receivedPackets = new List<byte[]>();
            var transport = new FakeConnectionTransport();
            var connection = new Connection(
                transport,
                _ => { },
                (packet, _) =>
                {
                    receivedPackets.Add(packet);
                    return ValueTask.CompletedTask;
                });
            connection.Close();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                connection.ReceiveFromTransportAsync(
                    PacketEncoder.Encode([0x01]),
                    CancellationToken.None).AsTask());
            Assert.Empty(receivedPackets);
        }

        [Fact]
        public void Send_ThrowsObjectDisposedException_WhenConnectionWasClosed()
        {
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, _ => { });
            connection.Close();

            Assert.Throws<ObjectDisposedException>(() => connection.Send([0x01]));
            Assert.Empty(transport.SentPackets);
        }

        [Fact]
        public async Task SendAsync_ThrowsObjectDisposedException_WhenConnectionWasClosed()
        {
            var transport = new FakeConnectionTransport();
            var connection = new Connection(transport, _ => { });
            connection.Close();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                connection.SendAsync([0x01], CancellationToken.None).AsTask());
            Assert.Empty(transport.SentPackets);
        }

        [Fact]
        public void Send_ThrowsObjectDisposedException_WhenTransportCloseFailed()
        {
            var closeException = new IOException("Close failed.");
            var transport = new FakeConnectionTransport
            {
                CloseException = closeException
            };
            var connection = new Connection(transport, _ => { });

            var actualException = Assert.Throws<IOException>(connection.Close);

            Assert.Same(closeException, actualException);
            Assert.Throws<ObjectDisposedException>(() => connection.Send([0x01]));
            Assert.Empty(transport.SentPackets);
        }

        private sealed class FakeConnectionTransport : IConnectionTransport
        {
            public List<byte[]> SentPackets { get; } = [];
            public bool IsClosed { get; private set; }
            public Exception? CloseException { get; init; }

            public void Send(byte[] data)
            {
                SentPackets.Add(data);
            }

            public ValueTask SendAsync(
                ReadOnlyMemory<byte> data,
                CancellationToken cancellationToken)
            {
                SentPackets.Add(data.ToArray());
                return ValueTask.CompletedTask;
            }

            public void Close()
            {
                if (CloseException is not null)
                {
                    throw CloseException;
                }

                IsClosed = true;
            }
        }
    }
}
