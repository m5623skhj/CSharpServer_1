using System.Buffers.Binary;

namespace CSharpServer.Packet
{
    public sealed class PacketBuffer
    {
        private const int HeaderSize = 4;
        private readonly int maxPayloadLength;
        private readonly List<byte> buffer = [];

        public PacketBuffer(int maxPayloadLength = ProtocolLimits.MaxPayloadLength)
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
                throw new InvalidDataException("Payload length cannot be negative.");
            }

            if (payloadLength > maxPayloadLength)
            {
                throw new InvalidDataException("Payload length exceeds max payload length.");
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
            return BinaryPrimitives.ReadInt32LittleEndian(buffer.GetRange(0, HeaderSize).ToArray());
        }
    }
}
