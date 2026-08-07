namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
        private static readonly TimeSpan MaxTimerDelay =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        private readonly Stream stream;
        private readonly byte[] buffer;
        private readonly Action<ReadOnlyMemory<byte>> dataHandler;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncDataHandler;
        private readonly SemaphoreSlim readSemaphore = new(1, 1);

        public StreamConnectionReader(
            Stream stream,
            int inBufferSize,
            Action<byte[]> dataHandler)
            : this(
                ValidateStream(stream),
                inBufferSize,
                CreateDataHandler(dataHandler),
                CreateAsyncDataHandler(dataHandler))
        {
        }

        internal StreamConnectionReader(
            Stream stream,
            int inBufferSize,
            Action<ReadOnlyMemory<byte>> dataHandler,
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncDataHandler)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(dataHandler);
            ArgumentNullException.ThrowIfNull(asyncDataHandler);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            this.stream = stream;
            buffer = new byte[inBufferSize];
            this.dataHandler = dataHandler;
            this.asyncDataHandler = asyncDataHandler;
        }

        internal int AvailableReadSlotCount => readSemaphore.CurrentCount;

        public bool ReadOnce()
        {
            readSemaphore.Wait();
            try
            {
                var readCount = stream.Read(buffer);
                return HandleRead(buffer, readCount);
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        public async Task<bool> ReadOnceAsync(CancellationToken cancellationToken)
        {
            await readSemaphore.WaitAsync(cancellationToken);
            try
            {
                var readCount = await stream.ReadAsync(buffer, cancellationToken);
                if (readCount == 0)
                {
                    return false;
                }

                await asyncDataHandler(buffer.AsMemory(0, readCount), cancellationToken);
                return true;
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        internal async Task<bool> ReadOnceAsync(
            CancellationToken cancellationToken,
            TimeSpan idleTimeout)
        {
            if (idleTimeout <= TimeSpan.Zero || idleTimeout > MaxTimerDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(idleTimeout));
            }

            await readSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var idleCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                idleCancellation.CancelAfter(idleTimeout);

                int readCount;
                try
                {
                    readCount = await stream.ReadAsync(buffer, idleCancellation.Token);
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested
                        && idleCancellation.IsCancellationRequested)
                {
                    return false;
                }

                if (readCount == 0)
                {
                    return false;
                }

                await asyncDataHandler(
                    buffer.AsMemory(0, readCount),
                    cancellationToken);
                return true;
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        private bool HandleRead(byte[] readBuffer, int readCount)
        {
            if (readCount == 0)
            {
                return false;
            }

            dataHandler(readBuffer.AsMemory(0, readCount));
            return true;
        }

        private static Stream ValidateStream(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return stream;
        }

        private static Action<ReadOnlyMemory<byte>> CreateDataHandler(
            Action<byte[]> dataHandler)
        {
            ArgumentNullException.ThrowIfNull(dataHandler);
            return data => dataHandler(data.ToArray());
        }

        private static Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>
            CreateAsyncDataHandler(Action<byte[]> dataHandler)
        {
            ArgumentNullException.ThrowIfNull(dataHandler);
            return (data, _) =>
            {
                dataHandler(data.ToArray());
                return ValueTask.CompletedTask;
            };
        }
    }
}
