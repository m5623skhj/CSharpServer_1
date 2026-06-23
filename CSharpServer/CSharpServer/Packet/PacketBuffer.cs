namespace CSharpServer.Packet
{
    public sealed class PacketBuffer
    {
        private const int HeaderSize = 4;
        private const int DefaultMaxPayloadLength = 4096;
        private readonly int maxPayloadLength;
        private readonly List<byte> buffer = [];

        public PacketBuffer(int maxPayloadLength = DefaultMaxPayloadLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPayloadLength);
            this.maxPayloadLength = maxPayloadLength;
        }

        public void Append(byte[] data)
        {
            buffer.AddRange(data);
        }

        public bool TryReadPacket(out byte[]? packet)
        {
            packet = null;
            if (buffer.Count < HeaderSize)
            {
                return false;
            }

            var payloadLength = ReadPayloadLength();
            if (payloadLength < 0)
            {
                throw new InvalidOperationException("Payload length cannot be negative.");
            }

            if (payloadLength > maxPayloadLength)
            {
                throw new InvalidOperationException("Payload length exceeds max payload length.");
            }

            if (buffer.Count < HeaderSize + payloadLength)
            {
                return false;
            }

            packet = buffer.GetRange(HeaderSize, payloadLength).ToArray();
            buffer.RemoveRange(0, HeaderSize + payloadLength);

            return true;
        }

        private int ReadPayloadLength()
        {
            return BitConverter.ToInt32(buffer.GetRange(0, HeaderSize).ToArray(), 0);
        }
    }
}
