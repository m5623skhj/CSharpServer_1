namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
        private readonly Stream stream;
        private readonly int bufferSize;
        private readonly Action<byte[]> dataHandler;
        private readonly SemaphoreSlim readSemaphore = new(1, 1);

        public StreamConnectionReader(Stream stream, int inBufferSize, Action<byte[]> dataHandler)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            this.stream = stream;
            bufferSize = inBufferSize;
            this.dataHandler = dataHandler;
        }

        public bool ReadOnce()
        {
            readSemaphore.Wait();
            try
            {
                var buffer = new byte[bufferSize];
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
                var buffer = new byte[bufferSize];
                var readCount = await stream.ReadAsync(buffer, cancellationToken);
                return HandleRead(buffer, readCount);
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        private bool HandleRead(byte[] buffer, int readCount)
        {
            if (readCount == 0)
            {
                return false;
            }

            dataHandler(buffer[..readCount]);
            return true;
        }
    }
}
