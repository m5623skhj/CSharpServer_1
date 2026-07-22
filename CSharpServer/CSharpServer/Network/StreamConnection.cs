namespace CSharpServer.Network
{
    public sealed class StreamConnection
    {
        private readonly Connection connection;
        private readonly StreamConnectionReader reader;

        public StreamConnection(Stream stream, int inBufferSize, Action<byte[]> packetHandler)
            : this(
                stream,
                inBufferSize,
                packetHandler,
                new StreamConnectionTransport(stream))
        {
        }

        internal StreamConnection(
            Stream stream,
            int inBufferSize,
            Action<byte[]> packetHandler,
            IConnectionTransport transport)
        {
            connection = new Connection(transport, packetHandler);
            reader = new StreamConnectionReader(stream, inBufferSize, connection.ReceiveFromTransport);
        }

        public bool ReadOnce()
        {
            return reader.ReadOnce();
        }

        public void ReadUntilEnd()
        {
            while (ReadOnce())
            {
            }
        }

        public async Task ReadUntilEndAsync(CancellationToken cancellationToken)
        {
            while (await reader.ReadOnceAsync(cancellationToken))
            {
            }
        }

        public async Task ReadUntilEndAsync(
            CancellationToken cancellationToken,
            TimeSpan idleTimeout)
        {
            if (idleTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(idleTimeout));
            }

            while (true)
            {
                using var idleCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);
                idleCancellation.CancelAfter(idleTimeout);

                try
                {
                    if (!await reader.ReadOnceAsync(idleCancellation.Token))
                    {
                        return;
                    }
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested
                        && idleCancellation.IsCancellationRequested)
                {
                    return;
                }
            }
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
