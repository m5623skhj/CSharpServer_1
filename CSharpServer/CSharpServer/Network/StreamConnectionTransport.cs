namespace CSharpServer.Network
{
    public sealed class StreamConnectionTransport : IConnectionTransport
    {
        private readonly Stream stream;
        private readonly object syncRoot = new();
        private bool isClosed;

        public StreamConnectionTransport(Stream stream)
        {
            this.stream = stream;
        }

        public void Send(byte[] data)
        {
            lock (syncRoot)
            {
                ObjectDisposedException.ThrowIf(isClosed, this);
                stream.Write(data);
            }
        }

        public void Close()
        {
            lock (syncRoot)
            {
                if (isClosed)
                {
                    return;
                }

                isClosed = true;
                stream.Close();
            }
        }
    }
}
