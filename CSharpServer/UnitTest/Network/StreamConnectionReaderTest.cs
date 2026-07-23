using System.Runtime.InteropServices;
using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionReaderTest
    {
        [Fact]
        public void ReadOnce_InvokesDataHandler_WithReadData()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            using var stream = new MemoryStream(data);
            var receivedData = new List<byte[]>();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                data => receivedData.Add(data.ToArray()));

            var result = reader.ReadOnce();

            Assert.True(result);
            var received = Assert.Single(receivedData);
            Assert.Equal(data, received);
        }

        [Fact]
        public void ReadOnce_DoesNotInvokeDataHandler_WhenStreamReturnsEndOfFile()
        {
            using var stream = new MemoryStream();
            var receivedData = new List<byte[]>();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                data => receivedData.Add(data.ToArray()));

            var result = reader.ReadOnce();

            Assert.False(result);
            Assert.Empty(receivedData);
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBufferSizeIsZero()
        {
            using var stream = new MemoryStream();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new StreamConnectionReader(stream, inBufferSize: 0, _ => { });
            });
        }

        [Fact]
        public async Task ReadOnceAsync_SerializesStreamReads_WhenCalledConcurrently()
        {
            using var stream = new ConcurrentAsyncReadTrackingStream();
            var receivedData = new List<byte[]>();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                data => receivedData.Add(data.ToArray()),
                (data, _) =>
                {
                    receivedData.Add(data.ToArray());
                    return ValueTask.CompletedTask;
                });
            var firstRead = reader.ReadOnceAsync(CancellationToken.None);

            await stream.FirstReadEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, reader.AvailableReadSlotCount);

            var secondRead = reader.ReadOnceAsync(CancellationToken.None);
            Assert.False(secondRead.IsCompleted);
            stream.AllowFirstReadToComplete.TrySetResult();
            await Task.WhenAll(firstRead, secondRead);

            Assert.False(stream.HadOverlappingReads);
            Assert.Single(receivedData);
            Assert.Equal(1, reader.AvailableReadSlotCount);
        }

        [Fact]
        public async Task ReadOnceAsync_StopsWaiting_WhenCancellationIsRequested()
        {
            using var stream = new CancellationAwareReadStream();
            using var cancellationTokenSource = new CancellationTokenSource();
            var receivedData = new List<byte[]>();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                data => receivedData.Add(data.ToArray()));
            var readTask = reader.ReadOnceAsync(cancellationTokenSource.Token);

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask);
            Assert.Empty(receivedData);
        }

        [Fact]
        public async Task ReadOnceAsync_ReusesReadBufferAcrossCalls()
        {
            using var stream = new ReadBufferTrackingStream();
            var reader = new StreamConnectionReader(stream, inBufferSize: 8, _ => { });

            Assert.True(await reader.ReadOnceAsync(CancellationToken.None));
            Assert.True(await reader.ReadOnceAsync(CancellationToken.None));

            Assert.Equal(2, stream.ReadBuffers.Count);
            Assert.Same(stream.ReadBuffers[0], stream.ReadBuffers[1]);
        }

        private sealed class ConcurrentAsyncReadTrackingStream : Stream
        {
            private int activeReadCount;
            private int readCallCount;

            public TaskCompletionSource FirstReadEntered { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource AllowFirstReadToComplete { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public bool HadOverlappingReads { get; private set; }

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
                if (Interlocked.Increment(ref activeReadCount) > 1)
                {
                    HadOverlappingReads = true;
                }

                try
                {
                    if (Interlocked.Increment(ref readCallCount) == 1)
                    {
                        FirstReadEntered.TrySetResult();
                        await AllowFirstReadToComplete.Task.WaitAsync(cancellationToken);
                        buffer.Span[0] = 0x01;
                        return 1;
                    }

                    return 0;
                }
                finally
                {
                    Interlocked.Decrement(ref activeReadCount);
                }
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

        private sealed class ReadBufferTrackingStream : Stream
        {
            private int readCount;

            public List<byte[]> ReadBuffers { get; } = [];
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

            public override int Read(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                Assert.True(MemoryMarshal.TryGetArray(
                    (ReadOnlyMemory<byte>)buffer,
                    out var segment));
                ReadBuffers.Add(segment.Array!);
                buffer.Span[0] = (byte)++readCount;
                return ValueTask.FromResult(1);
            }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }
    }
}
