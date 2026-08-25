using System.Runtime.InteropServices;
using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionReaderTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenStreamIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StreamConnectionReader(null!, inBufferSize: 8, _ => { }));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDataHandlerIsNull()
        {
            using var stream = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                new StreamConnectionReader(stream, inBufferSize: 8, null!));
        }

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
        public void ReadOnce_ClosesReader_WhenReadThrowsIOException()
        {
            var expectedException = new IOException("read failed");
            using var stream = new FailingReadStream(expectedException);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });

            var exception = Assert.Throws<IOException>(() => reader.ReadOnce());

            Assert.Same(expectedException, exception);
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            Assert.Throws<ObjectDisposedException>(() => reader.ReadOnce());
        }

        [Fact]
        public void ReadOnce_PreservesIOException_WhenClosingFailedReadFails()
        {
            var expectedException = new IOException("read failed");
            var stream = new FailingReadStream(
                expectedException,
                throwOnDispose: true);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });

            var exception = Assert.Throws<IOException>(() => reader.ReadOnce());

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            Assert.Throws<ObjectDisposedException>(() => reader.ReadOnce());
        }

        [Fact]
        public void ReadOnce_PreservesInvalidDataException_WhenClosingFailedHandlerFails()
        {
            var expectedException = new InvalidDataException("invalid packet");
            var stream = new CloseFailingMemoryStream([0x01]);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => throw expectedException);

            var exception = Assert.Throws<InvalidDataException>(() => reader.ReadOnce());

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            Assert.Throws<ObjectDisposedException>(() => reader.ReadOnce());
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
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ReadOnceAsync_PreservesCancellation_WhenClosingCanceledReadFails()
        {
            var stream = new CancellationAwareReadStream(throwOnDispose: true);
            using var cancellationTokenSource = new CancellationTokenSource();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });
            var readTask = reader.ReadOnceAsync(cancellationTokenSource.Token);

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellationTokenSource.CancelAsync();

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                readTask);
            Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsAssignableFrom<OperationCanceledException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ReadOnceAsync_ClosesReader_WhenIdleTimeoutExpires()
        {
            using var stream = new CancellationAwareReadStream();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });
            var readTask = reader.ReadOnceAsync(
                CancellationToken.None,
                TimeSpan.FromMilliseconds(50));

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.False(await readTask.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ReadOnceAsync_PreservesIdleTimeout_WhenClosingStreamFails()
        {
            var stream = new CancellationAwareReadStream(throwOnDispose: true);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });
            var readTask = reader.ReadOnceAsync(
                CancellationToken.None,
                TimeSpan.FromMilliseconds(50));

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            var exception = await Assert.ThrowsAsync<IOException>(() => readTask);
            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsAssignableFrom<OperationCanceledException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ReadOnceAsync_DoesNotCloseStream_WhenCanceledWhileWaitingForReadSlot()
        {
            using var stream = new ConcurrentAsyncReadTrackingStream();
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });
            var firstRead = reader.ReadOnceAsync(CancellationToken.None);

            await stream.FirstReadEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            using var cancellationTokenSource = new CancellationTokenSource();
            var waitingRead = reader.ReadOnceAsync(cancellationTokenSource.Token);
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingRead);
            Assert.False(stream.IsDisposed);

            stream.AllowFirstReadToComplete.TrySetResult();
            Assert.True(await firstRead);
            Assert.False(await reader.ReadOnceAsync(CancellationToken.None));

            Assert.False(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
        }

        [Fact]
        public async Task ReadOnceAsync_ClosesReader_WhenReadThrowsIOException()
        {
            var expectedException = new IOException("read failed");
            using var stream = new FailingReadStream(expectedException);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });

            var exception = await Assert.ThrowsAsync<IOException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));

            Assert.Same(expectedException, exception);
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ReadOnceAsync_PreservesIOException_WhenClosingFailedReadFails()
        {
            var expectedException = new IOException("read failed");
            var stream = new FailingReadStream(
                expectedException,
                throwOnDispose: true);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { });

            var exception = await Assert.ThrowsAsync<IOException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
            Assert.Equal(1, reader.AvailableReadSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadOnceAsync(CancellationToken.None));
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadOnceAsync_DoesNotCaptureSynchronizationContext(
            bool useIdleTimeout)
        {
            using var stream = new AsynchronouslyCompletingReadStream();
            var handlerStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var handlerCompletion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var reader = new StreamConnectionReader(
                stream,
                inBufferSize: 8,
                _ => { },
                (_, _) =>
                {
                    handlerStarted.TrySetResult();
                    return new ValueTask(handlerCompletion.Task);
                });
            var synchronizationContext = new QueueingSynchronizationContext();
            var readCompletion = new TaskCompletionSource<(bool Result, Exception? Exception)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var synchronousWaitStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var readThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    var readTask = useIdleTimeout
                        ? reader.ReadOnceAsync(
                            CancellationToken.None,
                            TimeSpan.FromSeconds(10))
                        : reader.ReadOnceAsync(CancellationToken.None);
                    synchronousWaitStarted.TrySetResult();
                    readCompletion.TrySetResult((
                        readTask.GetAwaiter().GetResult(),
                        null));
                }
                catch (Exception exception)
                {
                    readCompletion.TrySetResult((false, exception));
                }
            })
            {
                IsBackground = true
            };

            readThread.Start();
            await synchronousWaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            stream.CompleteRead(0x01);
            var readContinuation = await Task.WhenAny(
                    handlerStarted.Task,
                    synchronizationContext.ContinuationPosted.Task)
                .WaitAsync(TimeSpan.FromSeconds(1));

            try
            {
                Assert.Same(handlerStarted.Task, readContinuation);

                handlerCompletion.TrySetResult();
                var handlerContinuation = await Task.WhenAny(
                        readCompletion.Task,
                        synchronizationContext.ContinuationPosted.Task)
                    .WaitAsync(TimeSpan.FromSeconds(1));

                Assert.Same(readCompletion.Task, handlerContinuation);
                var completion = await readCompletion.Task;
                Assert.True(completion.Result);
                Assert.Null(completion.Exception);
                Assert.Equal(1, reader.AvailableReadSlotCount);
            }
            finally
            {
                handlerCompletion.TrySetResult();
                synchronizationContext.RunAll();
                Assert.True(readThread.Join(TimeSpan.FromSeconds(1)));
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

        private sealed class FailingReadStream : MemoryStream
        {
            private readonly IOException readException;
            private readonly bool throwOnDispose;

            public FailingReadStream(
                IOException readException,
                bool throwOnDispose = false)
            {
                this.readException = readException;
                this.throwOnDispose = throwOnDispose;
            }

            public bool IsDisposed { get; private set; }

            public override int Read(Span<byte> buffer)
            {
                throw readException;
            }

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                return ValueTask.FromException<int>(readException);
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

        private sealed class CloseFailingMemoryStream : MemoryStream
        {
            public CloseFailingMemoryStream(byte[] data)
                : base(data)
            {
            }

            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
                if (disposing)
                {
                    throw new IOException("close failed");
                }
            }
        }

        private sealed class AsynchronouslyCompletingReadStream : Stream
        {
            private readonly TaskCompletionSource<int> readCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            private Memory<byte> readBuffer;

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
                cancellationToken.ThrowIfCancellationRequested();
                readBuffer = buffer;
                return new ValueTask<int>(readCompletion.Task);
            }

            public void CompleteRead(byte value)
            {
                readBuffer.Span[0] = value;
                readCompletion.TrySetResult(1);
            }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
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
            public bool IsDisposed { get; private set; }

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
                ObjectDisposedException.ThrowIf(IsDisposed, this);
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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
            }
        }

        private sealed class CancellationAwareReadStream : Stream
        {
            private readonly bool throwOnDispose;

            public CancellationAwareReadStream(bool throwOnDispose = false)
            {
                this.throwOnDispose = throwOnDispose;
            }

            public TaskCompletionSource ReadStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public bool IsDisposed { get; private set; }

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
                ObjectDisposedException.ThrowIf(IsDisposed, this);
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
