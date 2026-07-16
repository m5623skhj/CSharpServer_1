using System.Net;
using System.Net.Sockets;
using CSharpServer.Content;

namespace CSharpServer.Network
{
    public sealed class EchoTcpServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly int bufferSize;

        public EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize)
        {
            listener = new TcpListener(ipAddress, port);
            bufferSize = inBufferSize;
        }

        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

        public void Start()
        {
            listener.Start();
        }

        public void AcceptAndHandleOnce()
        {
            using var client = listener.AcceptTcpClient();
            using var stream = client.GetStream();
            var connection = EchoStreamConnectionFactory.Create(stream, bufferSize);

            connection.ReadUntilEnd();
        }

        public void AcceptAndHandle(int clientCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clientCount);

            for (var i = 0; i < clientCount; i++)
            {
                AcceptAndHandleOnce();
            }
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
