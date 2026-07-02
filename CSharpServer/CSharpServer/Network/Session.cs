using CSharpServer.Packet;

namespace CSharpServer.Network
{
    public sealed class Session
    {
        private readonly PacketBuffer packetBuffer = new();
        private readonly Action<byte[]> packetHandler;

        public Session(Action<byte[]> packetHandler)
        {
            this.packetHandler = packetHandler;
        }

        public void Receive(byte[] data)
        {
            packetBuffer.Append(data);

            while (packetBuffer.TryReadPacket(out var packet) && packet is not null)
            {
                packetHandler(packet);
            }
        }
    }
}
