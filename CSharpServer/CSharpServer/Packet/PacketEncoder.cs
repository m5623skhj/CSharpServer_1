namespace CSharpServer.Packet
{
    public static class PacketEncoder
    {
        public static byte[] Encode(byte[] payload)
        {
            var packet = new byte[4 + payload.Length];

            BitConverter.GetBytes(payload.Length).CopyTo(packet, 0);
            payload.CopyTo(packet, 4);

            return packet;
        }
    }
}