namespace CSharpServer.Network
{
    public sealed class StreamConnectionTransport : IConnectionTransport
    {
        private readonly Stream stream;
        private readonly object closeSyncRoot = new();
        private readonly SemaphoreSlim sendSemaphore = new(1, 1);
        private bool isClosed;

        public StreamConnectionTransport(Stream stream)
        {
            this.stream = stream;
        }

        public void Send(byte[] data)
        {
            sendSemaphore.Wait();
            try
            {
                ThrowIfClosed();
                stream.Write(data);
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        public async ValueTask SendAsync(
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken)
        {
            await sendSemaphore.WaitAsync(cancellationToken);
            try
            {
                ThrowIfClosed();
                await stream.WriteAsync(data, cancellationToken);
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        public void Close()
        {
            lock (closeSyncRoot)
            {
                if (isClosed)
                {
                    return;
                }

                isClosed = true;
                stream.Close();
            }
        }

        private void ThrowIfClosed()
        {
            lock (closeSyncRoot)
            {
                ObjectDisposedException.ThrowIf(isClosed, this);
            }
        }
    }
}
