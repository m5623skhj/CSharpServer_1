using CSharpServer.Content;
using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class StreamConnectionTest
    {
        [Fact]
        public void ReadOnce_InvokesPacketHandler_WhenPacketIsReadFromStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream(PacketEncoder.Encode(payload));
            var receivedPackets = new List<byte[]>();
            var connection = new StreamConnection(stream, inBufferSize: 16, receivedPackets.Add);

            var result = connection.ReadOnce();

            Assert.True(result);
            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void ReadUntilEnd_InvokesPacketHandler_WhenPacketIsSplitAcrossMultipleReads()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream(PacketEncoder.Encode(payload));
            var receivedPackets = new List<byte[]>();
            var connection = new StreamConnection(stream, inBufferSize: 2, receivedPackets.Add);

            connection.ReadUntilEnd();

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public async Task ReadUntilEndAsync_InvokesPacketHandler_WhenPacketIsSplitAcrossMultipleReads()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream(PacketEncoder.Encode(payload));
            var receivedPackets = new List<byte[]>();
            var connection = new StreamConnection(stream, inBufferSize: 2, receivedPackets.Add);

            await connection.ReadUntilEndAsync(CancellationToken.None);

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public async Task ReadUntilEndAsync_StopsWaiting_WhenCancellationIsRequested()
        {
            using var stream = new CancellationAwareReadStream();
            using var cancellationTokenSource = new CancellationTokenSource();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });
            var readTask = connection.ReadUntilEndAsync(cancellationTokenSource.Token);

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask);
        }

        [Fact]
        public void ReadOnce_WritesEchoPacketToStream_WhenEchoHandlerIsUsed()
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
        public void Send_WritesEncodedPacketToStream()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            connection.Send(payload);

            Assert.Equal(PacketEncoder.Encode(payload), stream.ToArray());
        }

        [Fact]
        public void Close_ClosesStream()
        {
            using var stream = new TrackingStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            connection.Close();

            Assert.True(stream.IsDisposed);
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class CancellationAwareReadStream : Stream
        {
            public TaskCompletionSource ReadStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                ReadStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
