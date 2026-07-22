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
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

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
            HandleClient(listener.AcceptTcpClient());
        }

        public void AcceptAndHandle(int clientCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clientCount);

            for (var i = 0; i < clientCount; i++)
            {
                AcceptAndHandleOnce();
            }
        }

        public async Task AcceptAndHandleConcurrently(int clientCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clientCount);

            var clientTasks = new List<Task>();
            for (var i = 0; i < clientCount; i++)
            {
                var client = await listener.AcceptTcpClientAsync();
                clientTasks.Add(HandleClientAsync(client, CancellationToken.None));
            }

            await Task.WhenAll(clientTasks);
        }

        public async Task AcceptAndHandleConcurrently(CancellationToken cancellationToken)
        {
            var clientTasks = new List<Task>();
            var activeClients = new List<TcpClient>();
            var activeClientsLock = new object();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PruneCompletedClientTasks(clientTasks);
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    lock (activeClientsLock)
                    {
                        activeClients.Add(client);
                    }

                    clientTasks.Add(HandleTrackedClientAsync(
                        client,
                        activeClients,
                        activeClientsLock,
                        cancellationToken));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            CloseActiveClients(activeClients, activeClientsLock);
            await Task.WhenAll(clientTasks);
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var connection = EchoStreamConnectionFactory.Create(stream, bufferSize);
                    connection.ReadUntilEnd();
                }
            }
            catch (Exception exception) when (IsClientConnectionException(exception))
            {
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                {
                    await using var stream = client.GetStream();
                    var connection = EchoStreamConnectionFactory.Create(stream, bufferSize);
                    await connection.ReadUntilEndAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (IsClientConnectionException(exception))
            {
            }
        }

        private async Task HandleTrackedClientAsync(
            TcpClient client,
            List<TcpClient> activeClients,
            object activeClientsLock,
            CancellationToken cancellationToken)
        {
            try
            {
                await HandleClientAsync(client, cancellationToken);
            }
            finally
            {
                lock (activeClientsLock)
                {
                    activeClients.Remove(client);
                }
            }
        }

        private static void PruneCompletedClientTasks(List<Task> clientTasks)
        {
            clientTasks.RemoveAll(task => task.IsCompletedSuccessfully);
        }

        private static void CloseActiveClients(List<TcpClient> activeClients, object activeClientsLock)
        {
            lock (activeClientsLock)
            {
                foreach (var client in activeClients.ToArray())
                {
                    client.Close();
                }
            }
        }

        private static bool IsClientConnectionException(Exception exception)
        {
            return exception is IOException
                or SocketException
                or ObjectDisposedException
                or InvalidOperationException;
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
