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
        public async Task Create_SerializesEchoAndConnectionSendWrites()
        {
            var encodedPacket = PacketEncoder.Encode([0x01]);
            using var stream = new ConcurrentWriteTrackingStream(encodedPacket);
            var connection = EchoStreamConnectionFactory.Create(stream, inBufferSize: 16);
            var echoTask = Task.Run(connection.ReadOnce);

            await stream.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            using var sendStarted = new ManualResetEventSlim();
            var sendTask = Task.Run(() =>
            {
                sendStarted.Set();
                connection.Send([0x02]);
            });

            Assert.True(sendStarted.Wait(TimeSpan.FromSeconds(1)));
            var secondWriteEnteredEarly = stream.SecondWriteEntered.Wait(
                TimeSpan.FromMilliseconds(100));

            stream.AllowFirstWriteToComplete.Set();
            await Task.WhenAll(echoTask, sendTask);

            Assert.False(secondWriteEnteredEarly);
            Assert.False(stream.HadOverlappingWrites);
        }

        private sealed class ConcurrentWriteTrackingStream : Stream
        {
            private readonly MemoryStream readStream;
            private int activeWriteCount;
            private int writeCallCount;

            public ConcurrentWriteTrackingStream(byte[] readData)
            {
                readStream = new MemoryStream(readData);
            }

            public TaskCompletionSource FirstWriteStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public ManualResetEventSlim AllowFirstWriteToComplete { get; } = new();
            public ManualResetEventSlim SecondWriteEntered { get; } = new();
            public bool HadOverlappingWrites { get; private set; }

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

            public override int Read(byte[] buffer, int offset, int count)
            {
                return readStream.Read(buffer, offset, count);
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
                if (Interlocked.Increment(ref activeWriteCount) > 1)
                {
                    HadOverlappingWrites = true;
                }

                try
                {
                    if (Interlocked.Increment(ref writeCallCount) == 1)
                    {
                        FirstWriteStarted.TrySetResult();
                        AllowFirstWriteToComplete.Wait();
                    }
                    else
                    {
                        SecondWriteEntered.Set();
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeWriteCount);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    readStream.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
