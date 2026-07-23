using System.Net;
using System.Net.Sockets;
using CSharpClient;
using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class EchoTcpServerTest
    {
        [Fact]
        public async Task AcceptAndHandleOnce_ReturnsEchoResponseToClient()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Start();
            var serverTask = Task.Run(server.AcceptAndHandleOnce);
            var client = new EchoClient();

            var response = client.SendEchoRequest("127.0.0.1", server.Port, "hello");

            Assert.Equal("hello", response);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task AcceptAndHandle_ReturnsEchoResponsesToMultipleClientsSequentially()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Start();
            var serverTask = Task.Run(() => server.AcceptAndHandle(clientCount: 2));
            var client = new EchoClient();

            var firstResponse = client.SendEchoRequest("127.0.0.1", server.Port, "hello");
            var secondResponse = client.SendEchoRequest("127.0.0.1", server.Port, "world");

            Assert.Equal("hello", firstResponse);
            Assert.Equal("world", secondResponse);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ReturnsEchoResponsesToMultipleClients()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 2);
            var client = new EchoClient();

            var firstClientTask = Task.Run(() => client.SendEchoRequest("127.0.0.1", server.Port, "hello"));
            var secondClientTask = Task.Run(() => client.SendEchoRequest("127.0.0.1", server.Port, "world"));

            var responses = await Task.WhenAll(firstClientTask, secondClientTask).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Contains("hello", responses);
            Assert.Contains("world", responses);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ReturnsWhenCancellationIsRequested()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            using var cancellationTokenSource = new CancellationTokenSource();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(cancellationTokenSource.Token);
            var client = new EchoClient();

            var firstClientTask = Task.Run(() => client.SendEchoRequest("127.0.0.1", server.Port, "hello"));
            var secondClientTask = Task.Run(() => client.SendEchoRequest("127.0.0.1", server.Port, "world"));

            var responses = await Task.WhenAll(firstClientTask, secondClientTask).WaitAsync(TimeSpan.FromSeconds(5));
            await cancellationTokenSource.CancelAsync();

            Assert.Contains("hello", responses);
            Assert.Contains("world", responses);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ReturnsAfterCancellation_WhenAcceptedClientStaysOpen()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            using var cancellationTokenSource = new CancellationTokenSource();
            using var idleClient = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(cancellationTokenSource.Token);

            await idleClient.ConnectAsync(IPAddress.Loopback, server.Port);
            var stream = idleClient.GetStream();
            var packet = PacketEncoder.Encode([0x01]);
            await stream.WriteAsync(packet);
            var response = new byte[packet.Length];
            await stream.ReadExactlyAsync(response);
            await cancellationTokenSource.CancelAsync();

            Assert.Equal(packet, response);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Dispose_ClosesActiveClientsAndCompletesAcceptLoop()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromSeconds(5));
            using var client = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(CancellationToken.None);

            await client.ConnectAsync(IPAddress.Loopback, server.Port);
            var stream = client.GetStream();
            var packet = PacketEncoder.Encode([0x01]);
            await stream.WriteAsync(packet);
            var response = new byte[packet.Length];
            await stream.ReadExactlyAsync(response);

            server.Dispose();

            await serverTask.WaitAsync(TimeSpan.FromSeconds(1));
            var readCount = await stream.ReadAsync(new byte[1])
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, readCount);
            Assert.Equal(0, server.ActiveClientCount);
            Assert.Equal(1, server.AvailableClientSlotCount);
        }

        [Fact]
        public async Task Dispose_ClosesActiveClientsAndCompletesFixedCountAcceptLoop()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromSeconds(5));
            using var client = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 2);

            await client.ConnectAsync(IPAddress.Loopback, server.Port);
            var stream = client.GetStream();
            var packet = PacketEncoder.Encode([0x01]);
            await stream.WriteAsync(packet);
            var response = new byte[packet.Length];
            await stream.ReadExactlyAsync(response);

            server.Dispose();

            await serverTask.WaitAsync(TimeSpan.FromSeconds(1));
            var readCount = await stream.ReadAsync(new byte[1])
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, readCount);
            Assert.Equal(0, server.ActiveClientCount);
            Assert.Equal(1, server.AvailableClientSlotCount);
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_DoesNotExceedMaxConcurrentClients()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromSeconds(5));
            using var serverCancellation = new CancellationTokenSource();
            using var firstClient = new TcpClient();
            using var secondClient = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(serverCancellation.Token);

            try
            {
                await firstClient.ConnectAsync(IPAddress.Loopback, server.Port);
                var firstStream = firstClient.GetStream();
                var packet = PacketEncoder.Encode([0x01]);
                await firstStream.WriteAsync(packet);
                var firstResponse = new byte[packet.Length];
                await firstStream.ReadExactlyAsync(firstResponse);
                Assert.Equal(1, server.ActiveClientCount);
                Assert.Equal(0, server.AvailableClientSlotCount);
                Assert.True(SpinWait.SpinUntil(
                    () => server.WaitingClientSlotCount == 1,
                    TimeSpan.FromSeconds(1)));

                await secondClient.ConnectAsync(IPAddress.Loopback, server.Port);
                var secondStream = secondClient.GetStream();
                await secondStream.WriteAsync(packet);
                var secondResponse = new byte[packet.Length];
                Assert.Equal(1, server.ActiveClientCount);
                Assert.Equal(1, server.WaitingClientSlotCount);

                firstClient.Close();
                await secondStream.ReadExactlyAsync(secondResponse)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(packet, secondResponse);
            }
            finally
            {
                firstClient.Close();
                secondClient.Close();
                await serverCancellation.CancelAsync();
                await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ClosesClientAfterIdleTimeout()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromMilliseconds(100));
            using var serverCancellation = new CancellationTokenSource();
            using var idleClient = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(serverCancellation.Token);

            try
            {
                await idleClient.ConnectAsync(IPAddress.Loopback, server.Port);
                var buffer = new byte[1];

                var readCount = await idleClient.GetStream()
                    .ReadAsync(buffer)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5));

                Assert.Equal(0, readCount);
            }
            finally
            {
                await serverCancellation.CancelAsync();
                await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_PropagatesUnexpectedClientHandlerFailure()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromSeconds(5),
                clientHandler: (_, _) => Task.FromException(
                    new InvalidOperationException("handler failed")));
            using var cancellationTokenSource = new CancellationTokenSource();
            using var client = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(cancellationTokenSource.Token);

            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serverTask.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.Equal("handler failed", exception.Message);
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_WithClientCount_PropagatesHandlerFailureBeforeRemainingAccepts()
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 2,
                clientIdleTimeout: TimeSpan.FromSeconds(5),
                clientHandler: (_, _) => Task.FromException(
                    new InvalidOperationException("handler failed")));
            using var client = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 2);

            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serverTask.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.Equal("handler failed", exception.Message);
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_ContinuesAfterMalformedClientPacket()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            using var cancellationTokenSource = new CancellationTokenSource();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(cancellationTokenSource.Token);
            var client = new EchoClient();

            using (var malformedClient = new TcpClient())
            {
                await malformedClient.ConnectAsync(IPAddress.Loopback, server.Port);
                await malformedClient.GetStream().WriteAsync(new byte[] { 0x01, 0x10, 0x00, 0x00 });
            }

            var response = client.SendEchoRequest("127.0.0.1", server.Port, "hello");
            await cancellationTokenSource.CancelAsync();

            Assert.Equal("hello", response);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBufferSizeIsZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 0);
            });
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenMaxConcurrentClientsIsZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new EchoTcpServer(
                    IPAddress.Loopback,
                    port: 0,
                    inBufferSize: 2,
                    maxConcurrentClients: 0,
                    clientIdleTimeout: TimeSpan.FromSeconds(1));
            });
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenClientIdleTimeoutIsZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new EchoTcpServer(
                    IPAddress.Loopback,
                    port: 0,
                    inBufferSize: 2,
                    maxConcurrentClients: 1,
                    clientIdleTimeout: TimeSpan.Zero);
            });
        }
    }
}
