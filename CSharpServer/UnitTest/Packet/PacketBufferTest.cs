using CSharpServer.Packet;

namespace UnitTest.Packet
{
    public class PacketBufferTest
    {
        [Fact]
        public void TryReadPacket_ReturnFalse_WhenHeaderIsInComplete()
        {
            var buffer = new PacketBuffer();
            buffer.Append([0x05, 0x00]);

            var result = buffer.TryReadPacket(out var packet);

            Assert.False(result);
            Assert.Null(packet);
        }

        [Fact]
        public void TryReadPacket_ReturnsFalse_WhenPayloadIsInComplete()
        {
            var buffer = new PacketBuffer();
            buffer.Append([0x05, 0x00, 0x00, 0x00, 0x68, 0x65]);

            var result = buffer.TryReadPacket(out var packet);

            Assert.False(result);
            Assert.Null(packet);
        }

        [Fact]
        public void TryReadPacket_ReturnsTrueAndPayload_WhenSinglePacketIsComplete()
        {
            var buffer = new PacketBuffer();
            buffer.Append([0x05, 0x00, 0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F]);

            var result = buffer.TryReadPacket(out var packet);

            Assert.True(result);
            Assert.NotNull(packet);
            Assert.Equal(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F }, packet);
        }

        [Fact]
        public void TryReadPacket_ReturnsPacketsInOrder_WhenMultiplePacketsAreComplete()
        {
            var buffer = new PacketBuffer();
            buffer.Append([
                0x05, 0x00, 0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
                0x05, 0x00, 0x00, 0x00, 0x77, 0x6F, 0x72, 0x6C, 0x64
            ]);

            var firstResult = buffer.TryReadPacket(out var firstPacket);
            var secondResult = buffer.TryReadPacket(out var secondPacket);

            Assert.True(firstResult);
            Assert.Equal(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F }, firstPacket);

            Assert.True(secondResult);
            Assert.Equal(new byte[] { 0x77, 0x6F, 0x72, 0x6C, 0x64 }, secondPacket);
        }

        [Fact]
        public void TryReadPacket_ReturnsOnlyCompletePacket_WhenNextPacketHeaderIsIncomplete()
        {
            var buffer = new PacketBuffer();
            buffer.Append([
                0x05, 0x00, 0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
                0x05, 0x00
            ]);

            var firstResult = buffer.TryReadPacket(out var firstPacket);
            var secondResult = buffer.TryReadPacket(out var secondPacket);

            Assert.True(firstResult);
            Assert.Equal(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F }, firstPacket);

            Assert.False(secondResult);
            Assert.Null(secondPacket);
        }

        [Fact]
        public void TryReadPacket_ReturnsNextPacket_WhenRemainingDataCompletesIncompletePacket()
        {
            var buffer = new PacketBuffer();
            buffer.Append([
                0x05, 0x00, 0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
                0x05, 0x00
            ]);

            var firstResult = buffer.TryReadPacket(out var firstPacket);
            var secondResult = buffer.TryReadPacket(out var secondPacket);

            Assert.True(firstResult);
            Assert.Equal(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F }, firstPacket);

            Assert.False(secondResult);
            Assert.Null(secondPacket);

            buffer.Append([
                0x00, 0x00, 0x77, 0x6F, 0x72, 0x6C, 0x64
            ]);

            var thirdResult = buffer.TryReadPacket(out var thirdPacket);

            Assert.True(thirdResult);
            Assert.Equal(new byte[] { 0x77, 0x6F, 0x72, 0x6C, 0x64 }, thirdPacket);
        }

        [Fact]
        public void TryReadPacket_ThrowsInvalidOperationException_WhenPayloadLengthIsNegative()
        {
            var buffer = new PacketBuffer();
            buffer.Append([0xFF, 0xFF, 0xFF, 0xFF]);

            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.TryReadPacket(out _);
            });
        }

        [Fact]
        public void TryReadPacket_ThrowsInvalidOperationException_WhenPayloadLengthExceedsMaxPayloadLength()
        {
            var buffer = new PacketBuffer(maxPayloadLength: 4);
            buffer.Append([0x05, 0x00, 0x00, 0x00]);

            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.TryReadPacket(out _);
            });
        }
    }
}
