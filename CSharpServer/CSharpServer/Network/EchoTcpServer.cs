using System.Net;
using System.Net.Sockets;
using CSharpServer.Content;

namespace CSharpServer.Network
{
    public sealed class EchoTcpServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly int bufferSize;
        private readonly TimeSpan clientIdleTimeout;
        private readonly SemaphoreSlim clientSlots;
        private readonly Func<TcpClient, CancellationToken, Task> clientHandler;
        private int activeClientCount;

        public EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize)
            : this(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients: 100,
                clientIdleTimeout: TimeSpan.FromSeconds(30))
        {
        }

        public EchoTcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout)
            : this(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients,
                clientIdleTimeout,
                clientHandler: null)
        {
        }

        internal EchoTcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout,
            Func<TcpClient, CancellationToken, Task>? clientHandler)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentClients);
            if (clientIdleTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(clientIdleTimeout));
            }

            listener = new TcpListener(ipAddress, port);
            bufferSize = inBufferSize;
            this.clientIdleTimeout = clientIdleTimeout;
            clientSlots = new SemaphoreSlim(maxConcurrentClients, maxConcurrentClients);
            this.clientHandler = clientHandler ?? HandleClientAsync;
        }

        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        internal int ActiveClientCount => Volatile.Read(ref activeClientCount);

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
                await clientSlots.WaitAsync();
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    clientTasks.Add(HandleClientWithSlotAsync(client, CancellationToken.None));
                }
                catch
                {
                    clientSlots.Release();
                    throw;
                }
            }

            await Task.WhenAll(clientTasks);
        }

        public async Task AcceptAndHandleConcurrently(CancellationToken cancellationToken)
        {
            using var acceptCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            var clientTasks = new List<Task>();
            var activeClients = new List<TcpClient>();
            var activeClientsLock = new object();

            while (!acceptCancellation.IsCancellationRequested)
            {
                var slotAcquired = false;
                TcpClient? acceptedClient = null;
                try
                {
                    PruneCompletedClientTasks(clientTasks);
                    await clientSlots.WaitAsync(acceptCancellation.Token);
                    slotAcquired = true;
                    acceptedClient = await listener.AcceptTcpClientAsync(
                        acceptCancellation.Token);
                    lock (activeClientsLock)
                    {
                        activeClients.Add(acceptedClient);
                    }

                    var clientTask = HandleTrackedClientAsync(
                        acceptedClient,
                        activeClients,
                        activeClientsLock,
                        acceptCancellation.Token);
                    clientTasks.Add(clientTask);
                    _ = clientTask.ContinueWith(
                        static (_, state) => ((CancellationTokenSource)state!).Cancel(),
                        acceptCancellation,
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted
                            | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    acceptedClient = null;
                    slotAcquired = false;
                }
                catch (OperationCanceledException)
                    when (acceptCancellation.IsCancellationRequested)
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        clientSlots.Release();
                    }

                    break;
                }
                catch
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        clientSlots.Release();
                    }

                    throw;
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
                    await connection.ReadUntilEndAsync(cancellationToken, clientIdleTimeout);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (IsClientConnectionException(exception))
            {
            }
        }

        private async Task HandleClientWithSlotAsync(
            TcpClient client,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref activeClientCount);
            try
            {
                await clientHandler(client, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref activeClientCount);
                clientSlots.Release();
            }
        }

        private async Task HandleTrackedClientAsync(
            TcpClient client,
            List<TcpClient> activeClients,
            object activeClientsLock,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref activeClientCount);
            try
            {
                await clientHandler(client, cancellationToken);
            }
            finally
            {
                lock (activeClientsLock)
                {
                    activeClients.Remove(client);
                }

                Interlocked.Decrement(ref activeClientCount);
                clientSlots.Release();
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
                or InvalidDataException
                or SocketException
                or ObjectDisposedException;
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
