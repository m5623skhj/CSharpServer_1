using System.Net;
using System.Net.Sockets;

namespace CSharpServer.Network
{
    public class TcpServer : IDisposable
    {
        private static readonly TimeSpan MaxTimerDelay =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        private readonly TcpListener listener;
        private readonly int bufferSize;
        private readonly TimeSpan clientIdleTimeout;
        private readonly SemaphoreSlim clientSlots;
        private readonly Func<Stream, int, StreamConnection> connectionFactory;
        private readonly Func<TcpClient, CancellationToken, Task> clientHandler;
        private readonly CancellationTokenSource disposeCancellation = new();
        private readonly CancellationToken disposeToken;
        private readonly List<TcpClient> activeClients = [];
        private readonly object activeClientsLock = new();
        private readonly object lifecycleLock = new();
        private readonly object disposeSyncRoot = new();
        private int activeClientCount;
        private int waitingClientSlotCount;
        private int disposeState;
        private int clientSlotsDisposeState;
        private bool isStarted;

        public TcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            Func<Stream, int, StreamConnection> connectionFactory)
            : this(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients: 100,
                clientIdleTimeout: TimeSpan.FromSeconds(30),
                connectionFactory)
        {
        }

        public TcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout,
            Func<Stream, int, StreamConnection> connectionFactory)
            : this(
                ipAddress,
                port,
                inBufferSize,
                maxConcurrentClients,
                clientIdleTimeout,
                connectionFactory,
                clientHandler: null)
        {
        }

        internal TcpServer(
            IPAddress ipAddress,
            int port,
            int inBufferSize,
            int maxConcurrentClients,
            TimeSpan clientIdleTimeout,
            Func<Stream, int, StreamConnection> connectionFactory,
            Func<TcpClient, CancellationToken, Task>? clientHandler)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);
            ArgumentNullException.ThrowIfNull(connectionFactory);
            if (port is < 0 or > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentClients);
            if (clientIdleTimeout <= TimeSpan.Zero || clientIdleTimeout > MaxTimerDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(clientIdleTimeout));
            }

            listener = new TcpListener(ipAddress, port);
            bufferSize = inBufferSize;
            this.clientIdleTimeout = clientIdleTimeout;
            clientSlots = new SemaphoreSlim(maxConcurrentClients, maxConcurrentClients);
            this.connectionFactory = connectionFactory;
            this.clientHandler = clientHandler ?? HandleClientAsync;
            disposeToken = disposeCancellation.Token;
        }

        public int Port
        {
            get
            {
                lock (lifecycleLock)
                {
                    ThrowIfDisposed();
                    if (!isStarted)
                    {
                        throw new InvalidOperationException(
                            "The server listener has not been started.");
                    }

                    return ((IPEndPoint)listener.LocalEndpoint).Port;
                }
            }
        }
        internal int ActiveClientCount => Volatile.Read(ref activeClientCount);
        internal int AvailableClientSlotCount
        {
            get
            {
                ThrowIfDisposed();
                return clientSlots.CurrentCount;
            }
        }

        internal int WaitingClientSlotCount => Volatile.Read(ref waitingClientSlotCount);
        internal bool AreClientSlotsDisposed =>
            Volatile.Read(ref clientSlotsDisposeState) != 0;

        public void Start()
        {
            lock (lifecycleLock)
            {
                ThrowIfDisposed();
                listener.Start();
                isStarted = true;
            }
        }

        public void AcceptAndHandleOnce()
        {
            ThrowIfDisposed();
            var slotAcquired = false;
            try
            {
                WaitForClientSlot();
                slotAcquired = true;
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception exception)
                    when (IsDisposing()
                        && IsListenerShutdownException(exception))
                {
                    throw new ObjectDisposedException(nameof(TcpServer));
                }

                if (!TryTrackClient(client))
                {
                    client.Dispose();
                    throw new ObjectDisposedException(nameof(TcpServer));
                }

                HandleTrackedClient(client);
            }
            finally
            {
                if (slotAcquired)
                {
                    ReleaseClientSlot();
                }

                DisposeClientSlotsIfSafe();
            }
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
                disposeToken);
            var clientTasks = new List<Task>();
            using var closeClientsOnFailure = handlerFailureCancellation.Token.Register(
                CloseActiveClients);

            for (var i = 0; i < clientCount; i++)
            {
                var slotAcquired = false;
                TcpClient? acceptedClient = null;
                try
                {
                    await WaitForClientSlotAsync(handlerFailureCancellation.Token)
                        .ConfigureAwait(false);
                    slotAcquired = true;
                    acceptedClient = await listener.AcceptTcpClientAsync(
                            handlerFailureCancellation.Token)
                        .ConfigureAwait(false);
                    if (!TryTrackClient(acceptedClient))
                    {
                        acceptedClient.Dispose();
                        acceptedClient = null;
                        ReleaseClientSlot();
                        slotAcquired = false;
                        break;
                    }

                    var clientTask = HandleTrackedClientAsync(
                        acceptedClient,
                        handlerFailureCancellation.Token);
                    clientTasks.Add(clientTask);
                    CancelWhenNotCompletedSuccessfully(
                        clientTask,
                        handlerFailureCancellation);
                    acceptedClient = null;
                    slotAcquired = false;
                }
                catch (OperationCanceledException)
                    when (handlerFailureCancellation.IsCancellationRequested)
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    break;
                }
                catch (Exception exception)
                    when (IsDisposing()
                        && IsListenerShutdownException(exception))
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    break;
                }
                catch
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    handlerFailureCancellation.Cancel();
                    CloseActiveClients();
                    await ObserveClientTasksAsync(clientTasks).ConfigureAwait(false);
                    throw;
                }
            }

            await Task.WhenAll(clientTasks).ConfigureAwait(false);
            DisposeClientSlotsIfSafe();
        }

        public async Task AcceptAndHandleConcurrently(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            using var acceptCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                disposeToken);
            var clientTasks = new List<Task>();

            while (!acceptCancellation.IsCancellationRequested)
            {
                var slotAcquired = false;
                TcpClient? acceptedClient = null;
                try
                {
                    PruneCompletedClientTasks(clientTasks);
                    await WaitForClientSlotAsync(acceptCancellation.Token)
                        .ConfigureAwait(false);
                    slotAcquired = true;
                    acceptedClient = await listener.AcceptTcpClientAsync(
                            acceptCancellation.Token)
                        .ConfigureAwait(false);
                    if (!TryTrackClient(acceptedClient))
                    {
                        acceptedClient.Dispose();
                        acceptedClient = null;
                        ReleaseClientSlot();
                        slotAcquired = false;
                        break;
                    }

                    var clientTask = HandleTrackedClientAsync(
                        acceptedClient,
                        acceptCancellation.Token);
                    clientTasks.Add(clientTask);
                    CancelWhenNotCompletedSuccessfully(clientTask, acceptCancellation);
                    acceptedClient = null;
                    slotAcquired = false;
                }
                catch (OperationCanceledException)
                    when (acceptCancellation.IsCancellationRequested)
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    break;
                }
                catch (Exception exception)
                    when (IsDisposing()
                        && IsListenerShutdownException(exception))
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    break;
                }
                catch
                {
                    acceptedClient?.Dispose();
                    if (slotAcquired)
                    {
                        ReleaseClientSlot();
                    }

                    acceptCancellation.Cancel();
                    CloseActiveClients();
                    await ObserveClientTasksAsync(clientTasks).ConfigureAwait(false);
                    throw;
                }
            }

            CloseActiveClients();
            await Task.WhenAll(clientTasks).ConfigureAwait(false);
            DisposeClientSlotsIfSafe();
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
                    var connection = connectionFactory(stream, bufferSize)
                        ?? throw new InvalidOperationException(
                            "Connection factory returned null.");
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
                    var connection = connectionFactory(stream, bufferSize)
                        ?? throw new InvalidOperationException(
                            "Connection factory returned null.");
                    await connection.ReadUntilEndAsync(cancellationToken, clientIdleTimeout)
                        .ConfigureAwait(false);
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
                await clientHandler(client, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                UntrackClient(client);
                Interlocked.Decrement(ref activeClientCount);
                ReleaseClientSlot();
                DisposeClientSlotsIfSafe();
            }
        }

        private async Task WaitForClientSlotAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref waitingClientSlotCount);
            try
            {
                await clientSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref waitingClientSlotCount);
            }
        }

        private void WaitForClientSlot()
        {
            Interlocked.Increment(ref waitingClientSlotCount);
            try
            {
                try
                {
                    clientSlots.Wait(disposeToken);
                }
                catch (Exception exception)
                    when (IsDisposing()
                        && exception is OperationCanceledException
                            or ObjectDisposedException)
                {
                    throw new ObjectDisposedException(nameof(TcpServer));
                }
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
                await Task.WhenAll(clientTasks).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static void CancelWhenNotCompletedSuccessfully(
            Task clientTask,
            CancellationTokenSource cancellationTokenSource)
        {
            _ = clientTask.ContinueWith(
                static (_, state) => ((CancellationTokenSource)state!).Cancel(),
                cancellationTokenSource,
                CancellationToken.None,
                TaskContinuationOptions.NotOnRanToCompletion
                    | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void ReleaseClientSlot()
        {
            try
            {
                clientSlots.Release();
            }
            catch (ObjectDisposedException) when (IsDisposing())
            {
            }
        }

        private void DisposeClientSlotsIfSafe()
        {
            if (!IsDisposing()
                || ActiveClientCount != 0
                || WaitingClientSlotCount != 0
                || Interlocked.Exchange(ref clientSlotsDisposeState, 1) != 0)
            {
                return;
            }

            clientSlots.Dispose();
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
            lock (disposeSyncRoot)
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
                    lock (lifecycleLock)
                    {
                        listener.Stop();
                        isStarted = false;
                    }

                    CloseActiveClients();
                    disposeCancellation.Dispose();
                    DisposeClientSlotsIfSafe();
                }
            }
        }

        private bool IsDisposing()
        {
            return Volatile.Read(ref disposeState) != 0;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(
                IsDisposing(),
                this);
        }
    }
}
