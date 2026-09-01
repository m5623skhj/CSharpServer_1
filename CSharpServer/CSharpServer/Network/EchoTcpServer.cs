using System.Net;
using System.Net.Sockets;
using CSharpServer.Content;

namespace CSharpServer.Network
{
    public sealed class EchoTcpServer : TcpServer
    {
        public EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize)
            : base(
                ipAddress,
                port,
                inBufferSize,
                EchoStreamConnectionFactory.Create)
        {
        }

        public EchoTcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout)
            : base(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients,
                clientIdleTimeout,
                EchoStreamConnectionFactory.Create)
        {
        }

        internal EchoTcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout,
            Func<TcpClient, CancellationToken, Task>? clientHandler)
            : base(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients,
                clientIdleTimeout,
                EchoStreamConnectionFactory.Create,
                clientHandler)
        {
        }
    }
}
