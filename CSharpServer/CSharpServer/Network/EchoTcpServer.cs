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
        private readonly CancellationTokenSource disposeCancellation = new();
        private readonly List<TcpClient> activeClients = [];
        private readonly object activeClientsLock = new();
        private int activeClientCount;
        private int waitingClientSlotCount;
        private int disposeState;

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
        internal int AvailableClientSlotCount => clientSlots.CurrentCount;
        internal int WaitingClientSlotCount => Volatile.Read(ref waitingClientSlotCount);

        public void Start()
        {
            ThrowIfDisposed();
            listener.Start();
        }

        public void AcceptAndHandleOnce()
        {
            ThrowIfDisposed();
            var client = listener.AcceptTcpClient();
            if (!TryTrackClient(client))
            {
                client.Dispose();
                throw new ObjectDisposedException(nameof(EchoTcpServer));
            }

            HandleTrackedClient(client);
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

            ThrowIfDisposed();
            using var handlerFailureCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                disposeCancellation.Token);
            var clientTasks = new List<Task>();
            using var closeClientsOnFailure = handlerFailureCancellation.Token.Register(
                CloseActiveClients);

            for (var i = 0; i < clientCount; i++)
            {
                var slotAcquired = false;
                TcpClient? acceptedClient = null;
                try
                {
                    await WaitForClientSlotAsync(handlerFailureCancellation.Token);
                    slotAcquired = true;
                    acceptedClient = await listener.AcceptTcpClientAsync(
                        handlerFailureCancellation.Token);
                    if (!TryTrackClient(acceptedClient))
                    {
                        acceptedClient.Dispose();
                        acceptedClient = null;
                        clientSlots.Release();
                        slotAcquired = false;
                        break;
                    }

                    var clientTask = HandleTrackedClientAsync(
                        acceptedClient,
                        handlerFailureCancellation.Token);
                    clientTasks.Add(clientTask);
                    CancelWhenFaulted(clientTask, handlerFailureCancellation);
                    acceptedClient = null;
                    slotAcquired = false;
                }
                catch (OperationCanceledException)
                    when (handlerFailureCancellation.IsCancellationRequested)
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        clientSlots.Release();
                    }

                    break;
                }
                catch (Exception exception)
                    when (disposeCancellation.IsCancellationRequested
                        && IsListenerShutdownException(exception))
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

                    handlerFailureCancellation.Cancel();
                    CloseActiveClients();
                    await ObserveClientTasksAsync(clientTasks);
                    throw;
                }
            }

            await Task.WhenAll(clientTasks);
        }

        public async Task AcceptAndHandleConcurrently(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            using var acceptCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                disposeCancellation.Token);
            var clientTasks = new List<Task>();

            while (!acceptCancellation.IsCancellationRequested)
            {
                var slotAcquired = false;
                TcpClient? acceptedClient = null;
                try
                {
                    PruneCompletedClientTasks(clientTasks);
                    await WaitForClientSlotAsync(acceptCancellation.Token);
                    slotAcquired = true;
                    acceptedClient = await listener.AcceptTcpClientAsync(
                        acceptCancellation.Token);
                    if (!TryTrackClient(acceptedClient))
                    {
                        acceptedClient.Dispose();
                        acceptedClient = null;
                        clientSlots.Release();
                        slotAcquired = false;
                        break;
                    }

                    var clientTask = HandleTrackedClientAsync(
                        acceptedClient,
                        acceptCancellation.Token);
                    clientTasks.Add(clientTask);
                    CancelWhenFaulted(clientTask, acceptCancellation);
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
                catch (Exception exception)
                    when (disposeCancellation.IsCancellationRequested
                        && IsListenerShutdownException(exception))
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

                    acceptCancellation.Cancel();
                    CloseActiveClients();
                    await ObserveClientTasksAsync(clientTasks);
                    throw;
                }
            }

            CloseActiveClients();
            await Task.WhenAll(clientTasks);
        }

        private void HandleTrackedClient(TcpClient client)
        {
            Interlocked.Increment(ref activeClientCount);
            try
            {
                HandleClient(client);
            }
            finally
            {
                UntrackClient(client);
                Interlocked.Decrement(ref activeClientCount);
            }
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

        private async Task HandleTrackedClientAsync(
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
                UntrackClient(client);
                Interlocked.Decrement(ref activeClientCount);
                clientSlots.Release();
            }
        }

        private async Task WaitForClientSlotAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref waitingClientSlotCount);
            try
            {
                await clientSlots.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref waitingClientSlotCount);
            }
        }

        private bool TryTrackClient(TcpClient client)
        {
            lock (activeClientsLock)
            {
                if (Volatile.Read(ref disposeState) != 0)
                {
                    return false;
                }

                activeClients.Add(client);
                return true;
            }
        }

        private void UntrackClient(TcpClient client)
        {
            lock (activeClientsLock)
            {
                activeClients.Remove(client);
            }
        }

        private static void PruneCompletedClientTasks(List<Task> clientTasks)
        {
            clientTasks.RemoveAll(task => task.IsCompletedSuccessfully);
        }

        private static async Task ObserveClientTasksAsync(List<Task> clientTasks)
        {
            try
            {
                await Task.WhenAll(clientTasks);
            }
            catch
            {
            }
        }

        private static void CancelWhenFaulted(
            Task clientTask,
            CancellationTokenSource cancellationTokenSource)
        {
            _ = clientTask.ContinueWith(
                static (_, state) => ((CancellationTokenSource)state!).Cancel(),
                cancellationTokenSource,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void CloseActiveClients()
        {
            TcpClient[] clients;
            lock (activeClientsLock)
            {
                clients = activeClients.ToArray();
            }

            foreach (var client in clients)
            {
                client.Close();
            }
        }

        private static bool IsClientConnectionException(Exception exception)
        {
            return exception is IOException
                or InvalidDataException
                or SocketException
                or ObjectDisposedException;
        }

        private static bool IsListenerShutdownException(Exception exception)
        {
            return exception is InvalidOperationException
                or SocketException
                or ObjectDisposedException;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeState, 1) != 0)
            {
                return;
            }

            try
            {
                disposeCancellation.Cancel();
            }
            finally
            {
                listener.Stop();
                CloseActiveClients();
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref disposeState) != 0,
                this);
        }
    }
}
