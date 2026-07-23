using CSharpServer.Content;
using CSharpServer.Packet;

namespace UnitTest.Content
{
    public class EchoStreamConnectionFactoryTest
    {
        [Fact]
        public void Create_ReturnsConnectionThatWritesEchoPacketToStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var encodedPacket = PacketEncoder.Encode(payload);
            using var stream = new MemoryStream();
            stream.Write(encodedPacket);
            stream.Position = 0;

            var connection = EchoStreamConnectionFactory.Create(stream, inBufferSize: 16);

            connection.ReadOnce();

            Assert.Equal(encodedPacket.Concat(encodedPacket), stream.ToArray());
        }

        [Fact]
        public void Create_UsesSameStreamForEchoAndConnectionSend()
        {
            var encodedPacket = PacketEncoder.Encode([0x01]);
            var sentPacket = PacketEncoder.Encode([0x02]);
            using var stream = new MemoryStream();
            stream.Write(encodedPacket);
            stream.Position = 0;
            var connection = EchoStreamConnectionFactory.Create(stream, inBufferSize: 16);

            connection.ReadOnce();
            connection.Send([0x02]);

            Assert.Equal(
                encodedPacket.Concat(encodedPacket).Concat(sentPacket),
                stream.ToArray());
        }

        [Fact]
        public async Task Create_UsesAsyncWriteDuringAsyncReadLoop()
        {
            var encodedPacket = PacketEncoder.Encode([0x01]);
            using var stream = new AsyncEchoStream(encodedPacket);
            var connection = EchoStreamConnectionFactory.Create(stream, inBufferSize: 16);

            await connection.ReadUntilEndAsync(CancellationToken.None);

            Assert.Equal(encodedPacket, stream.WrittenData);
        }

        private sealed class AsyncEchoStream : Stream
        {
            private readonly MemoryStream readStream;
            private readonly MemoryStream writeStream = new();

            public AsyncEchoStream(byte[] readData)
            {
                readStream = new MemoryStream(readData);
            }

            public byte[] WrittenData => writeStream.ToArray();
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                return readStream.ReadAsync(buffer, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                return writeStream.WriteAsync(buffer, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    readStream.Dispose();
                    writeStream.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
