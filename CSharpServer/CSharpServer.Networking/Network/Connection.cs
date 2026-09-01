namespace CSharpServer.Network
{
    public sealed class Connection : IConnectionSender
    {
        private readonly Session session;
        private readonly IConnectionTransport transport;
        private int closedState;

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

        public Connection(
            IConnectionTransport transport,
            IConnectionPacketHandler packetHandler)
        {
            ArgumentNullException.ThrowIfNull(transport);
            ArgumentNullException.ThrowIfNull(packetHandler);

            this.transport = transport;
            session = new Session(
                packet => packetHandler.Handle(this, packet),
                transport.Send,
                (packet, cancellationToken) =>
                    packetHandler.HandleAsync(this, packet, cancellationToken),
                transport.SendAsync);
        }

        internal Connection(
            IConnectionTransport transport,
            Action<byte[]> packetHandler,
            Func<byte[], CancellationToken, ValueTask> asyncPacketHandler)
        {
            ArgumentNullException.ThrowIfNull(transport);
            ArgumentNullException.ThrowIfNull(packetHandler);
            ArgumentNullException.ThrowIfNull(asyncPacketHandler);

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
            ThrowIfClosed();
            session.Send(payload);
        }

        public ValueTask SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return session.SendAsync(payload, cancellationToken);
        }

        public void Close()
        {
            Interlocked.Exchange(ref closedState, 1);
            session.MarkReceiveUnusable();
            transport.Close();
        }

        private void ThrowIfClosed()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref closedState) != 0,
                this);
        }
    }
}
