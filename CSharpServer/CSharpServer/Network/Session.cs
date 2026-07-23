using CSharpServer.Packet;

namespace CSharpServer.Network
{
    public sealed class Session
    {
        private readonly PacketBuffer packetBuffer = new();
        private readonly Action<byte[]> packetHandler;
        private readonly Action<byte[]> packetSender;
        private readonly Func<byte[], CancellationToken, ValueTask> asyncPacketHandler;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncPacketSender;
        private readonly SemaphoreSlim receiveSemaphore = new(1, 1);

        public Session(Action<byte[]> packetHandler)
            : this(packetHandler, _ => { })
        {
        }

        public Session(Action<byte[]> packetHandler, Action<byte[]> packetSender)
            : this(
                packetHandler,
                packetSender,
                (packet, _) =>
                {
                    packetHandler(packet);
                    return ValueTask.CompletedTask;
                },
                (packet, _) =>
                {
                    packetSender(packet.ToArray());
                    return ValueTask.CompletedTask;
                })
        {
        }

        internal Session(
            Action<byte[]> packetHandler,
            Action<byte[]> packetSender,
            Func<byte[], CancellationToken, ValueTask> asyncPacketHandler,
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncPacketSender)
        {
            this.packetHandler = packetHandler;
            this.packetSender = packetSender;
            this.asyncPacketHandler = asyncPacketHandler;
            this.asyncPacketSender = asyncPacketSender;
        }

        public void Receive(byte[] data)
        {
            Receive((ReadOnlyMemory<byte>)data);
        }

        public void Receive(ReadOnlyMemory<byte> data)
        {
            receiveSemaphore.Wait();
            try
            {
                packetBuffer.Append(data.Span);

                while (packetBuffer.TryReadPacket(out var packet) && packet is not null)
                {
                    packetHandler(packet);
                }
            }
            finally
            {
                receiveSemaphore.Release();
            }
        }

        public async ValueTask ReceiveAsync(
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken)
        {
            await receiveSemaphore.WaitAsync(cancellationToken);
            try
            {
                packetBuffer.Append(data.Span);

                while (packetBuffer.TryReadPacket(out var packet) && packet is not null)
                {
                    await asyncPacketHandler(packet, cancellationToken);
                }
            }
            finally
            {
                receiveSemaphore.Release();
            }
        }

        public void Send(byte[] payload)
        {
            packetSender(PacketEncoder.Encode(payload));
        }

        public ValueTask SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            return asyncPacketSender(PacketEncoder.Encode(payload), cancellationToken);
        }
    }
}
