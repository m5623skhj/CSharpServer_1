using CSharpServer.Packet;

namespace CSharpServer.Network
{
    public sealed class Session
    {
        private readonly PacketBuffer packetBuffer = new();
        private readonly Action<byte[]> packetHandler;
        private readonly Action<byte[]> packetSender;
        private readonly object receiveSyncRoot = new();

        public Session(Action<byte[]> packetHandler)
            : this(packetHandler, _ => { })
        {
        }

        public Session(Action<byte[]> packetHandler, Action<byte[]> packetSender)
        {
            this.packetHandler = packetHandler;
            this.packetSender = packetSender;
        }

        public void Receive(byte[] data)
        {
            lock (receiveSyncRoot)
            {
                packetBuffer.Append(data);

                while (packetBuffer.TryReadPacket(out var packet) && packet is not null)
                {
                    packetHandler(packet);
                }
            }
        }

        public void Send(byte[] payload)
        {
            packetSender(PacketEncoder.Encode(payload));
        }
    }
}
