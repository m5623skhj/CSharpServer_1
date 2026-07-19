using System.Net;
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
    }
}
