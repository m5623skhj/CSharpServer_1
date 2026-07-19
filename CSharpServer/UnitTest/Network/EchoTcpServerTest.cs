using System.Net;
using System.Net.Sockets;
using CSharpClient;
using CSharpServer.Network;

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
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await cancellationTokenSource.CancelAsync();

            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
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
    }
}
