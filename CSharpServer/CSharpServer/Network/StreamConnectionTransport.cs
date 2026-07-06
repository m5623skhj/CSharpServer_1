namespace CSharpServer.Network
{
    public sealed class StreamConnectionTransport : IConnectionTransport
    {
        private readonly Stream stream;

        public StreamConnectionTransport(Stream stream)
        {
            this.stream = stream;
        }

        public void Send(byte[] data)
        {
            stream.Write(data);
        }

        public void Close()
        {
            stream.Close();
        }
    }
}
