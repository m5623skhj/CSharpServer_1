namespace CSharpServer.Content
{
    public sealed class EchoPacketHandler
    {
        private readonly Action<byte[]> packetSender;

        public EchoPacketHandler(Action<byte[]> packetSender)
        {
            this.packetSender = packetSender;
        }

        public void Handle(byte[] payload)
        {
            packetSender(payload);
        }
    }
}
