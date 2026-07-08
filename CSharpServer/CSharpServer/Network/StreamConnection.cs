namespace CSharpServer.Network
{
    public sealed class StreamConnection
    {
        private readonly Connection connection;
        private readonly StreamConnectionReader reader;

        public StreamConnection(Stream stream, int inBufferSize, Action<byte[]> packetHandler)
        {
            var transport = new StreamConnectionTransport(stream);
            connection = new Connection(transport, packetHandler);
            reader = new StreamConnectionReader(stream, inBufferSize, connection.ReceiveFromTransport);
        }

        public bool ReadOnce()
        {
            return reader.ReadOnce();
        }

        public void Send(byte[] payload)
        {
            connection.Send(payload);
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
