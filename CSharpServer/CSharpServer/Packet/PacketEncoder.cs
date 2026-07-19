using System.Buffers.Binary;

namespace CSharpServer.Packet
{
    public static class PacketEncoder
    {
        public static byte[] Encode(byte[] payload)
        {
            var packet = new byte[4 + payload.Length];

            BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), payload.Length);
            payload.CopyTo(packet, 4);

            return packet;
        }
    }
}
