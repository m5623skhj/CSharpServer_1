namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
        private readonly Stream stream;
        private readonly int bufferSize;
        private readonly Action<byte[]> dataHandler;

        public StreamConnectionReader(Stream stream, int inBufferSize, Action<byte[]> dataHandler)
        {
            this.stream = stream;
            bufferSize = inBufferSize;
            this.dataHandler = dataHandler;
        }

        public void ReadOnce()
        {
            var buffer = new byte[bufferSize];
            var readCount = stream.Read(buffer);
            if (readCount == 0)
            {
                return;
            }

            var data = buffer[..readCount];

            dataHandler(data);
        }
    }
}
