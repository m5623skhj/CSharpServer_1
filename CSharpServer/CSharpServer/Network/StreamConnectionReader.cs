namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
        private readonly Stream stream;
        private readonly int bufferSize;
        private readonly Action<byte[]> dataHandler;
        private readonly object readSyncRoot = new();

        public StreamConnectionReader(Stream stream, int inBufferSize, Action<byte[]> dataHandler)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            this.stream = stream;
            bufferSize = inBufferSize;
            this.dataHandler = dataHandler;
        }

        public bool ReadOnce()
        {
            lock (readSyncRoot)
            {
                var buffer = new byte[bufferSize];
                var readCount = stream.Read(buffer);
                if (readCount == 0)
                {
                    return false;
                }

                var data = buffer[..readCount];

                dataHandler(data);
                return true;
            }
        }
    }
}
