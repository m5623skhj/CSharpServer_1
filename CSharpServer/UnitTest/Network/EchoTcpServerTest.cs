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
        public async Task AcceptAndHandleOnce_ReturnsEmptyEchoResponseToClient()
        {
            using var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Start();
            var serverTask = Task.Run(server.AcceptAndHandleOnce);
            var client = new EchoClient();

            var response = client.SendEchoRequest("127.0.0.1", server.Port, string.Empty);

            Assert.Equal(string.Empty, response);
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
        public async Task Dispose_DisposesClientSlots_WhenAsyncHandlerFaultsDuringShutdown()
        {
            var handlerEntered = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowHandlerFailure = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2,
                maxConcurrentClients: 1,
                clientIdleTimeout: TimeSpan.FromSeconds(5),
                clientHandler: async (_, _) =>
                {
                    handlerEntered.TrySetResult();
                    await allowHandlerFailure.Task;
                    throw new InvalidOperationException("handler failed");
                });
            using var client = new TcpClient();
            server.Start();
            var serverTask = server.AcceptAndHandleConcurrently(clientCount: 1);

            await client.ConnectAsync(IPAddress.Loopback, server.Port);
            await handlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            server.Dispose();
            allowHandlerFailure.TrySetResult();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serverTask.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.Equal("handler failed", exception.Message);
            Assert.True(server.AreClientSlotsDisposed);
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

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenPortIsOutsideRange(
            int port)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EchoTcpServer(IPAddress.Loopback, port, inBufferSize: 2));

            Assert.Equal("port", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBufferSizeIsNotPositive(
            int inBufferSize)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize));

            Assert.Equal("inBufferSize", exception.ParamName);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenIpAddressIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new EchoTcpServer(null!, port: 0, inBufferSize: 2));
            Assert.Equal("ipAddress", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenMaxConcurrentClientsIsNotPositive(
            int maxConcurrentClients)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EchoTcpServer(
                    IPAddress.Loopback,
                    port: 0,
                    inBufferSize: 2,
                    maxConcurrentClients,
                    clientIdleTimeout: TimeSpan.FromSeconds(1)));

            Assert.Equal("maxConcurrentClients", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenClientIdleTimeoutIsNotPositive(
            int clientIdleTimeoutMilliseconds)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EchoTcpServer(
                    IPAddress.Loopback,
                    port: 0,
                    inBufferSize: 2,
                    maxConcurrentClients: 1,
                    clientIdleTimeout: TimeSpan.FromMilliseconds(
                        clientIdleTimeoutMilliseconds)));

            Assert.Equal("clientIdleTimeout", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AcceptAndHandle_ThrowsArgumentOutOfRangeException_WhenClientCountIsNotPositive(
            int clientCount)
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                server.AcceptAndHandle(clientCount));

            Assert.Equal("clientCount", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AcceptAndHandleConcurrently_ThrowsArgumentOutOfRangeException_WhenClientCountIsNotPositive(
            int clientCount)
        {
            using var server = new EchoTcpServer(
                IPAddress.Loopback,
                port: 0,
                inBufferSize: 2);

            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                server.AcceptAndHandleConcurrently(clientCount));

            Assert.Equal("clientCount", exception.ParamName);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);

            server.Dispose();
            var exception = Record.Exception(server.Dispose);

            Assert.Null(exception);
        }

        [Fact]
        public void Start_ThrowsObjectDisposedException_WhenServerIsDisposed()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Dispose();

            Assert.Throws<ObjectDisposedException>(server.Start);
        }

        [Fact]
        public async Task Start_DoesNotRestartListener_WhenDisposeRunsConcurrently()
        {
            const int iterationCount = 1000;

            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
                using var startGate = new Barrier(participantCount: 3);
                var startTask = Task.Run(() =>
                {
                    startGate.SignalAndWait();
                    try
                    {
                        server.Start();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                });
                var disposeTask = Task.Run(() =>
                {
                    startGate.SignalAndWait();
                    server.Dispose();
                });

                startGate.SignalAndWait();
                await Task.WhenAll(startTask, disposeTask);

                Assert.Equal(0, server.Port);
            }
        }

        [Fact]
        public void AcceptAndHandleOnce_ThrowsObjectDisposedException_WhenServerIsDisposed()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Dispose();

            Assert.Throws<ObjectDisposedException>(server.AcceptAndHandleOnce);
        }

        [Fact]
        public void AcceptAndHandle_ThrowsObjectDisposedException_WhenServerIsDisposed()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Dispose();

            Assert.Throws<ObjectDisposedException>(() => server.AcceptAndHandle(clientCount: 1));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_WithClientCount_ThrowsObjectDisposedException_WhenServerIsDisposed()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                server.AcceptAndHandleConcurrently(clientCount: 1));
        }

        [Fact]
        public async Task AcceptAndHandleConcurrently_WithCancellation_ThrowsObjectDisposedException_WhenServerIsDisposed()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            server.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                server.AcceptAndHandleConcurrently(CancellationToken.None));
        }

        [Fact]
        public void Dispose_DisposesClientSlots_WhenNoAcceptLoopIsRunning()
        {
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);

            server.Dispose();

            Assert.Throws<ObjectDisposedException>(() => server.AvailableClientSlotCount);
        }

        [Fact]
        public async Task Dispose_DisposesClientSlots_AfterSynchronousHandlerCompletes()
        {
            using var client = new TcpClient();
            var server = new EchoTcpServer(IPAddress.Loopback, port: 0, inBufferSize: 2);
            try
            {
                server.Start();
                var serverTask = Task.Run(server.AcceptAndHandleOnce);

                await client.ConnectAsync(IPAddress.Loopback, server.Port);
                Assert.True(SpinWait.SpinUntil(
                    () => server.ActiveClientCount == 1,
                    TimeSpan.FromSeconds(1)));

                server.Dispose();

                await serverTask.WaitAsync(TimeSpan.FromSeconds(1));
                Assert.Throws<ObjectDisposedException>(() => server.AvailableClientSlotCount);
            }
            finally
            {
                server.Dispose();
            }
        }
    }
}
