using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionTransportTest
    {
        [Fact]
        public void Send_WritesDataToStream()
        {
            using var stream = new MemoryStream();
            var transport = new StreamConnectionTransport(stream);
            var data = new byte[] { 0x01, 0x02, 0x03 };

            transport.Send(data);

            Assert.Equal(data, stream.ToArray());
        }

        [Fact]
        public async Task SendAsync_PropagatesCancellationToStreamWrite()
        {
            using var stream = new CancellationAwareWriteStream();
            var transport = new StreamConnectionTransport(stream);
            using var cancellationTokenSource = new CancellationTokenSource();
            var sendTask = transport.SendAsync(
                new byte[] { 0x01 },
                cancellationTokenSource.Token).AsTask();

            await stream.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
        }

        [Fact]
        public async Task SendAsync_SerializesConcurrentWrites()
        {
            using var stream = new ConcurrentAsyncWriteStream();
            var transport = new StreamConnectionTransport(stream);
            var firstSend = transport.SendAsync(new byte[] { 0x01 }, CancellationToken.None)
                .AsTask();

            await stream.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, transport.AvailableSendSlotCount);

            var secondSend = transport.SendAsync(new byte[] { 0x02 }, CancellationToken.None)
                .AsTask();
            Assert.False(secondSend.IsCompleted);
            stream.AllowFirstWriteToComplete.TrySetResult();

            await Task.WhenAll(firstSend, secondSend);
            Assert.False(stream.HadOverlappingWrites);
            Assert.Equal(1, transport.AvailableSendSlotCount);
        }

        [Fact]
        public void Close_ClosesStream()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            transport.Close();

            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void Close_ClosesStreamOnlyOnce_WhenCalledMultipleTimes()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            transport.Close();
            transport.Close();

            Assert.Equal(1, stream.DisposeCount);
        }

        [Fact]
        public async Task Close_ReturnsWhileActiveSendIsBlocked()
        {
            using var stream = new BlockingWriteStream();
            var transport = new StreamConnectionTransport(stream);
            var sendTask = Task.Run(() => transport.Send([0x01]));

            Assert.True(stream.WriteStarted.Wait(TimeSpan.FromSeconds(1)));

            var closeTask = Task.Run(transport.Close);
            await closeTask.WaitAsync(TimeSpan.FromSeconds(1));

            stream.AllowWriteToComplete.Set();
            await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => sendTask);

            Assert.True(stream.CloseCalled.IsSet);
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool IsDisposed { get; private set; }
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        private sealed class BlockingWriteStream : MemoryStream
        {
            public ManualResetEventSlim WriteStarted { get; } = new();
            public ManualResetEventSlim AllowWriteToComplete { get; } = new();
            public ManualResetEventSlim CloseCalled { get; } = new();

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteStarted.Set();
                AllowWriteToComplete.Wait();
                base.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                CloseCalled.Set();
                base.Dispose(disposing);
            }
        }

        private sealed class CancellationAwareWriteStream : Stream
        {
            public TaskCompletionSource WriteStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public override bool CanRead => false;
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

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override async ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                WriteStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
        }

        private sealed class ConcurrentAsyncWriteStream : Stream
        {
            private int activeWriteCount;
            private int writeCount;

            public TaskCompletionSource FirstWriteStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource AllowFirstWriteToComplete { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public bool HadOverlappingWrites { get; private set; }

            public override bool CanRead => false;
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

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override async ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                if (Interlocked.Increment(ref activeWriteCount) > 1)
                {
                    HadOverlappingWrites = true;
                }

                try
                {
                    if (Interlocked.Increment(ref writeCount) == 1)
                    {
                        FirstWriteStarted.TrySetResult();
                        await AllowFirstWriteToComplete.Task.WaitAsync(cancellationToken);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeWriteCount);
                }
            }
        }
    }
}
