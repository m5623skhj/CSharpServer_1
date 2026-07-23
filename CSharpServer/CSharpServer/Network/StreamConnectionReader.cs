namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
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
                stream,
                inBufferSize,
                data => dataHandler(data.ToArray()),
                (data, _) =>
                {
                    dataHandler(data.ToArray());
                    return ValueTask.CompletedTask;
                })
        {
        }

        internal StreamConnectionReader(
            Stream stream,
            int inBufferSize,
            Action<ReadOnlyMemory<byte>> dataHandler,
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncDataHandler)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            this.stream = stream;
            buffer = new byte[inBufferSize];
            this.dataHandler = dataHandler;
            this.asyncDataHandler = asyncDataHandler;
        }

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

        private bool HandleRead(byte[] readBuffer, int readCount)
        {
            if (readCount == 0)
            {
                return false;
            }

            dataHandler(readBuffer.AsMemory(0, readCount));
            return true;
        }
    }
}
