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
    }
}
