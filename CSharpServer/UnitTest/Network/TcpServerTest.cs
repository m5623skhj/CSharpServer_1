using System.Net;
using System.Net.Sockets;
using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class TcpServerTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConnectionFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TcpServer(
                    IPAddress.Loopback,
                    port: 0,
                    inBufferSize: 16,
                    connectionFactory: null!));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_UsesInjectedConnectionFactory()
        {
            var factoryCallCount = 0;
            using var server = new TcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                (stream, inBufferSize) =>
                {
                    Interlocked.Increment(ref factoryCallCount);
                    return new StreamConnection(
                        stream,
                        inBufferSize,
                        new IncrementingPacketHandler());
                });
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 1);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, server.Port);
            await using var stream = client.GetStream();
            await stream.WriteAsync(PacketEncoder.Encode([0x01]));
            var response = new byte[5];

            await stream.ReadExactlyAsync(response);
            client.Close();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(PacketEncoder.Encode([0x02]), response);
            Assert.Equal(1, factoryCallCount);
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ThrowsInvalidOperationException_WhenFactoryReturnsNull()
        {
            using var server = new TcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                (_, _) => null!);
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 1);
            using var client = new TcpClient();

            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serverTask.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal("Connection factory returned null.", exception.Message);
        }

        private sealed class IncrementingPacketHandler : IConnectionPacketHandler
        {
            public void Handle(IConnectionSender sender, byte[] payload)
            {
                sender.Send(Increment(payload));
            }

            public ValueTask HandleAsync(
                IConnectionSender sender,
                byte[] payload,
                CancellationToken cancellationToken)
            {
                return sender.SendAsync(Increment(payload), cancellationToken);
            }

            private static byte[] Increment(byte[] payload)
            {
                return payload.Select(value => checked((byte)(value + 1))).ToArray();
            }
        }
    }
}
