namespace CSharpServer.Network
{
    public sealed class Connection
    {
        private readonly Session session;
        private readonly IConnectionTransport transport;

        public Connection(IConnectionTransport transport, Action<byte[]> packetHandler)
        {
            this.transport = transport;
            session = new Session(packetHandler, transport.Send);
        }

        public void ReceiveFromTransport(byte[] data)
        {
            session.Receive(data);
        }

        public void Send(byte[] payload)
        {
            session.Send(payload);
        }

        public void Close()
        {
            transport.Close();
        }
    }
}
