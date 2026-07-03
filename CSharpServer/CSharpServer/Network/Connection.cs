namespace CSharpServer.Network
{
    public sealed class Connection
    {
        private readonly Session session;

        public Connection(IConnectionTransport transport, Action<byte[]> packetHandler)
        {
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
    }
}
