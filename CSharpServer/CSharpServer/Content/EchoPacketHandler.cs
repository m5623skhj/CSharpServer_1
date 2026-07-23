namespace CSharpServer.Content
{
    public sealed class EchoPacketHandler
    {
        private readonly Action<byte[]> packetSender;
        private readonly Func<byte[], CancellationToken, ValueTask> asyncPacketSender;

        public EchoPacketHandler(Action<byte[]> packetSender)
            : this(
                packetSender,
                (payload, _) =>
                {
                    packetSender(payload);
                    return ValueTask.CompletedTask;
                })
        {
        }

        internal EchoPacketHandler(
            Action<byte[]> packetSender,
            Func<byte[], CancellationToken, ValueTask> asyncPacketSender)
        {
            this.packetSender = packetSender;
            this.asyncPacketSender = asyncPacketSender;
        }

        public void Handle(byte[] payload)
        {
            packetSender(payload);
        }

        public ValueTask HandleAsync(byte[] payload, CancellationToken cancellationToken)
        {
            return asyncPacketSender(payload, cancellationToken);
        }
    }
}
