using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionTransportTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenStreamIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StreamConnectionTransport(null!));
        }

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
        public void Send_FlushesStreamAfterWrite()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            transport.Send([0x01]);

            Assert.Equal(1, stream.FlushCount);
            Assert.Equal(new[] { "write", "flush" }, stream.Operations);
        }

        [Fact]
        public void Send_ThrowsArgumentNullException_WhenDataIsNull()
        {
            using var stream = new MemoryStream();
            var transport = new StreamConnectionTransport(stream);

            var exception = Assert.Throws<ArgumentNullException>(() =>
                transport.Send(null!));
            Assert.Equal("data", exception.ParamName);
        }

        [Fact]
        public void Send_ThrowsObjectDisposedException_WhenTransportIsClosed()
        {
            using var stream = new MemoryStream();
            var transport = new StreamConnectionTransport(stream);
            transport.Close();

            Assert.Throws<ObjectDisposedException>(() =>
                transport.Send([0x01]));
            Assert.Equal(1, transport.AvailableSendSlotCount);
        }

        [Fact]
        public void Send_ClosesTransport_WhenWriteThrowsIOException()
        {
            var expectedException = new IOException("write failed");
            using var stream = new FailingWriteStream(expectedException);
            var transport = new StreamConnectionTransport(stream);

            var exception = Assert.Throws<IOException>(() =>
                transport.Send([0x01]));

            Assert.Same(expectedException, exception);
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            Assert.Throws<ObjectDisposedException>(() =>
                transport.Send([0x02]));
        }

        [Fact]
        public void Send_PreservesIOException_WhenClosingFailedWriteFails()
        {
            var expectedException = new IOException("write failed");
            var stream = new FailingWriteStream(
                expectedException,
                throwOnDispose: true);
            var transport = new StreamConnectionTransport(stream);

            var exception = Assert.Throws<IOException>(() =>
                transport.Send([0x01]));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            Assert.Throws<ObjectDisposedException>(() =>
                transport.Send([0x02]));
        }

        [Fact]
        public void Send_ClosesTransport_WhenWriteIsCanceled()
        {
            using var stream = new CancellationAwareWriteStream();
            var transport = new StreamConnectionTransport(stream);

            Assert.ThrowsAny<OperationCanceledException>(() =>
                transport.Send([0x01]));

            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            Assert.Throws<ObjectDisposedException>(() =>
                transport.Send([0x02]));
        }

        [Fact]
        public void Send_PreservesCancellation_WhenClosingCanceledWriteFails()
        {
            var stream = new CancellationAwareWriteStream(throwOnDispose: true);
            var transport = new StreamConnectionTransport(stream);

            var exception = Assert.ThrowsAny<OperationCanceledException>(() =>
                transport.Send([0x01]));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsAssignableFrom<OperationCanceledException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            Assert.Throws<ObjectDisposedException>(() =>
                transport.Send([0x02]));
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
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.SendAsync(
                    new byte[] { 0x02 },
                    CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task SendAsync_PreservesCancellation_WhenClosingCanceledWriteFails()
        {
            var stream = new CancellationAwareWriteStream(throwOnDispose: true);
            var transport = new StreamConnectionTransport(stream);
            using var cancellationTokenSource = new CancellationTokenSource();
            var sendTask = transport.SendAsync(
                new byte[] { 0x01 },
                cancellationTokenSource.Token).AsTask();

            await stream.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellationTokenSource.CancelAsync();

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sendTask);
            Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsAssignableFrom<OperationCanceledException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
        }

        [Fact]
        public async Task SendAsync_DoesNotCloseStream_WhenCanceledWhileWaitingForSendSlot()
        {
            using var stream = new ConcurrentAsyncWriteStream();
            var transport = new StreamConnectionTransport(stream);
            var firstSend = transport.SendAsync(
                new byte[] { 0x01 },
                CancellationToken.None).AsTask();

            await stream.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            using var cancellationTokenSource = new CancellationTokenSource();
            var waitingSend = transport.SendAsync(
                new byte[] { 0x02 },
                cancellationTokenSource.Token).AsTask();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingSend);
            Assert.False(stream.IsDisposed);

            stream.AllowFirstWriteToComplete.TrySetResult();
            await firstSend;
            await transport.SendAsync(
                new byte[] { 0x03 },
                CancellationToken.None);

            Assert.False(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
        }

        [Fact]
        public async Task SendAsync_FlushesStreamAfterWrite()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            await transport.SendAsync(new byte[] { 0x01 }, CancellationToken.None);

            Assert.Equal(1, stream.FlushCount);
            Assert.Equal(new[] { "write", "flush" }, stream.Operations);
        }

        [Fact]
        public async Task SendAsync_DoesNotCaptureSynchronizationContext()
        {
            using var stream = new AsynchronouslyCompletingWriteStream();
            var transport = new StreamConnectionTransport(stream);
            var synchronizationContext = new QueueingSynchronizationContext();
            var sendCompletion = new TaskCompletionSource<Exception?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var synchronousWaitStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sendThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    var sendTask = transport.SendAsync(
                            new byte[] { 0x01 },
                            CancellationToken.None)
                        .AsTask();
                    synchronousWaitStarted.TrySetResult();
                    sendTask.GetAwaiter().GetResult();
                    sendCompletion.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    sendCompletion.TrySetResult(exception);
                }
            })
            {
                IsBackground = true
            };

            sendThread.Start();
            await synchronousWaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            stream.CompleteWrite();
            var firstCompletion = await Task.WhenAny(
                    sendCompletion.Task,
                    synchronizationContext.ContinuationPosted.Task)
                .WaitAsync(TimeSpan.FromSeconds(1));

            try
            {
                Assert.Same(sendCompletion.Task, firstCompletion);
                Assert.Null(await sendCompletion.Task);
            }
            finally
            {
                synchronizationContext.RunAll();
                Assert.True(sendThread.Join(TimeSpan.FromSeconds(1)));
            }
        }

        [Fact]
        public async Task SendAsync_ThrowsObjectDisposedException_WhenTransportIsClosed()
        {
            using var stream = new MemoryStream();
            var transport = new StreamConnectionTransport(stream);
            transport.Close();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.SendAsync(new byte[] { 0x01 }, CancellationToken.None).AsTask());
            Assert.Equal(1, transport.AvailableSendSlotCount);
        }

        [Fact]
        public async Task SendAsync_ClosesTransport_WhenWriteThrowsIOException()
        {
            var expectedException = new IOException("write failed");
            using var stream = new FailingWriteStream(expectedException);
            var transport = new StreamConnectionTransport(stream);

            var exception = await Assert.ThrowsAsync<IOException>(() =>
                transport.SendAsync(
                    new byte[] { 0x01 },
                    CancellationToken.None).AsTask());

            Assert.Same(expectedException, exception);
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.SendAsync(
                    new byte[] { 0x02 },
                    CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task SendAsync_PreservesIOException_WhenClosingFailedWriteFails()
        {
            var expectedException = new IOException("write failed");
            var stream = new FailingWriteStream(
                expectedException,
                throwOnDispose: true);
            var transport = new StreamConnectionTransport(stream);

            var exception = await Assert.ThrowsAsync<IOException>(() =>
                transport.SendAsync(
                    new byte[] { 0x01 },
                    CancellationToken.None).AsTask());

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, transport.AvailableSendSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.SendAsync(
                    new byte[] { 0x02 },
                    CancellationToken.None).AsTask());
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
            public int FlushCount { get; private set; }
            public List<string> Operations { get; } = [];

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                Operations.Add("write");
                base.Write(buffer);
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Operations.Add("write");
                base.Write(buffer.Span);
                return ValueTask.CompletedTask;
            }

            public override void Flush()
            {
                Operations.Add("flush");
                FlushCount++;
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Operations.Add("flush");
                FlushCount++;
                return Task.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        private sealed class FailingWriteStream : MemoryStream
        {
            private readonly IOException writeException;
            private readonly bool throwOnDispose;

            public FailingWriteStream(
                IOException writeException,
                bool throwOnDispose = false)
            {
                this.writeException = writeException;
                this.throwOnDispose = throwOnDispose;
            }

            public bool IsDisposed { get; private set; }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                throw writeException;
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                return ValueTask.FromException(writeException);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
                if (disposing && throwOnDispose)
                {
                    throw new IOException("close failed");
                }
            }
        }

        private sealed class QueueingSynchronizationContext : SynchronizationContext
        {
            private readonly Queue<(SendOrPostCallback Callback, object? State)> callbacks = [];
            private readonly object callbacksLock = new();

            public TaskCompletionSource ContinuationPosted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public override void Post(SendOrPostCallback callback, object? state)
            {
                lock (callbacksLock)
                {
                    callbacks.Enqueue((callback, state));
                }

                ContinuationPosted.TrySetResult();
            }

            public void RunAll()
            {
                while (true)
                {
                    (SendOrPostCallback Callback, object? State) continuation;
                    lock (callbacksLock)
                    {
                        if (!callbacks.TryDequeue(out continuation))
                        {
                            return;
                        }
                    }

                    continuation.Callback(continuation.State);
                }
            }
        }

        private sealed class AsynchronouslyCompletingWriteStream : Stream
        {
            private readonly TaskCompletionSource writeCompletion = new(
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

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask(writeCompletion.Task);
            }

            public void CompleteWrite()
            {
                writeCompletion.TrySetResult();
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
            private readonly bool throwOnDispose;

            public CancellationAwareWriteStream(bool throwOnDispose = false)
            {
                this.throwOnDispose = throwOnDispose;
            }

            public TaskCompletionSource WriteStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public bool IsDisposed { get; private set; }

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
                throw new OperationCanceledException("write canceled");

            public override async ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                WriteStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
                if (disposing && throwOnDispose)
                {
                    throw new IOException("close failed");
                }
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
            public bool IsDisposed { get; private set; }

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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
            }
        }
    }
}
