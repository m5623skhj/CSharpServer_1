namespace CSharpServer.Network
{
    public sealed class Connection
    {
        private readonly Session session;
        private readonly IConnectionTransport transport;

        public Connection(IConnectionTransport transport, Action<byte[]> packetHandler)
            : this(
                transport,
                packetHandler,
                (packet, _) =>
                {
                    packetHandler(packet);
                    return ValueTask.CompletedTask;
                })
        {
        }

        internal Connection(
            IConnectionTransport transport,
            Action<byte[]> packetHandler,
            Func<byte[], CancellationToken, ValueTask> asyncPacketHandler)
        {
            this.transport = transport;
            session = new Session(
                packetHandler,
                transport.Send,
                asyncPacketHandler,
                transport.SendAsync);
        }

        public void ReceiveFromTransport(ReadOnlyMemory<byte> data)
        {
            session.Receive(data);
        }

        public ValueTask ReceiveFromTransportAsync(
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken)
        {
            return session.ReceiveAsync(data, cancellationToken);
        }

        public void Send(byte[] payload)
        {
            session.Send(payload);
        }

        public ValueTask SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            return session.SendAsync(payload, cancellationToken);
        }

        public void Close()
        {
            transport.Close();
        }
    }
}
