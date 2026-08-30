using CSharpServer.Content;
using CSharpServer.Network;
using CSharpServer.Packet;

namespace UnitTest.Network
{
    public class StreamConnectionTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenStreamIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new StreamConnection(null!, inBufferSize: 16, _ => { }));

            Assert.Equal("stream", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBufferSizeIsNotPositive(
            int inBufferSize)
        {
            using var stream = new MemoryStream();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StreamConnection(stream, inBufferSize, _ => { }));

            Assert.Equal("inBufferSize", exception.ParamName);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPacketHandlerIsNull()
        {
            using var stream = new MemoryStream();

            var exception = Assert.Throws<ArgumentNullException>(() =>
                new StreamConnection(stream, inBufferSize: 16, null!));

            Assert.Equal("packetHandler", exception.ParamName);
        }

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
        public async Task ReadUntilEndAsync_WithIdleTimeout_PassesCallerCancellationToPacketHandler()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream(PacketEncoder.Encode(payload));
            using var cancellation = new CancellationTokenSource();
            var handlerCancellationToken = CancellationToken.None;
            var transport = new StreamConnectionTransport(Stream.Null);
            var connection = new StreamConnection(
                stream,
                inBufferSize: 16,
                _ => { },
                (_, cancellationToken) =>
                {
                    handlerCancellationToken = cancellationToken;
                    return ValueTask.CompletedTask;
                },
                transport);

            await connection.ReadUntilEndAsync(
                cancellation.Token,
                idleTimeout: TimeSpan.FromSeconds(1));

            Assert.Equal(cancellation.Token, handlerCancellationToken);
        }

        [Fact]
        public async Task ReadUntilEndAsync_WithIdleTimeout_ReturnsWhenReadIsIdle()
        {
            using var stream = new CancellationAwareReadStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            var readTask = connection.ReadUntilEndAsync(
                CancellationToken.None,
                idleTimeout: TimeSpan.FromMilliseconds(50));

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await readTask.WaitAsync(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ReadUntilEndAsync_WithIdleTimeout_PropagatesCallerCancellation()
        {
            using var stream = new CancellationAwareReadStream();
            using var cancellation = new CancellationTokenSource();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });
            var readTask = connection.ReadUntilEndAsync(
                cancellation.Token,
                idleTimeout: TimeSpan.FromSeconds(5));

            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask);
        }

        [Fact]
        public async Task ReadUntilEndAsync_ThrowsArgumentOutOfRangeException_WhenIdleTimeoutExceedsTimerLimit()
        {
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 2, _ => { });

            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                connection.ReadUntilEndAsync(CancellationToken.None, TimeSpan.MaxValue));

            Assert.Equal("idleTimeout", exception.ParamName);
        }

        [Fact]
        public async Task ReadUntilEndAsync_AllowsIdleTimeoutAtTimerLimit()
        {
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 2, _ => { });

            await connection.ReadUntilEndAsync(
                CancellationToken.None,
                TimeSpan.FromMilliseconds(uint.MaxValue - 1));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadUntilEndAsync_DoesNotCaptureSynchronizationContext(
            bool useIdleTimeout)
        {
            using var stream = new AsynchronouslyCompletingEofStream();
            var context = new QueueingSynchronizationContext();
            var readStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var completion = new TaskCompletionSource<Exception?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(context);
                try
                {
                    var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });
                    var readTask = useIdleTimeout
                        ? connection.ReadUntilEndAsync(
                            CancellationToken.None,
                            TimeSpan.FromSeconds(5))
                        : connection.ReadUntilEndAsync(CancellationToken.None);
                    readStarted.TrySetResult();
                    readTask.GetAwaiter().GetResult();
                    completion.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    completion.TrySetResult(exception);
                }
            })
            {
                IsBackground = true
            };

            thread.Start();
            try
            {
                await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
                stream.CompleteRead();

                var completedTask = await Task.WhenAny(
                        completion.Task,
                        context.ContinuationPosted.Task)
                    .WaitAsync(TimeSpan.FromSeconds(1));

                Assert.Same(completion.Task, completedTask);
                Assert.Null(await completion.Task);
            }
            finally
            {
                context.RunQueuedCallbacks();
                Assert.True(thread.Join(TimeSpan.FromSeconds(1)));
            }
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
        public void Send_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            var exception = Assert.Throws<ArgumentNullException>(() =>
                connection.Send(null!));

            Assert.Equal("payload", exception.ParamName);
        }

        [Fact]
        public void SendAsync_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            using var stream = new MemoryStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            void SendNullPayload()
            {
                _ = connection.SendAsync(null!, CancellationToken.None);
            }

            var exception = Assert.Throws<ArgumentNullException>(SendNullPayload);

            Assert.Equal("payload", exception.ParamName);
        }

        [Fact]
        public async Task SendAsync_WritesEncodedPacketAndPassesCancellationToken()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new AsyncWriteTrackingStream();
            using var cancellation = new CancellationTokenSource();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            await connection.SendAsync(payload, cancellation.Token);

            Assert.Equal(PacketEncoder.Encode(payload), stream.WrittenData);
            Assert.Equal(cancellation.Token, stream.WriteCancellationToken);
        }

        [Fact]
        public void Close_ClosesStream()
        {
            using var stream = new TrackingStream();
            var connection = new StreamConnection(stream, inBufferSize: 16, _ => { });

            connection.Close();

            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void ReadOnce_ThrowsObjectDisposedException_WhenConnectionWasClosed()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new CloseTolerantReadStream(PacketEncoder.Encode(payload));
            var receivedPackets = new List<byte[]>();
            var connection = new StreamConnection(stream, inBufferSize: 16, receivedPackets.Add);

            connection.Close();

            Assert.Throws<ObjectDisposedException>(() => connection.ReadOnce());
            Assert.Empty(receivedPackets);
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

        private sealed class CloseTolerantReadStream : MemoryStream
        {
            public CloseTolerantReadStream(byte[] buffer)
                : base(buffer)
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        private sealed class AsyncWriteTrackingStream : MemoryStream
        {
            public byte[] WrittenData { get; private set; } = [];
            public CancellationToken WriteCancellationToken { get; private set; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("Synchronous writes are not expected.");
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                WrittenData = buffer.ToArray();
                WriteCancellationToken = cancellationToken;
                return ValueTask.CompletedTask;
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

            public void RunQueuedCallbacks()
            {
                while (true)
                {
                    (SendOrPostCallback Callback, object? State) callback;
                    lock (callbacksLock)
                    {
                        if (!callbacks.TryDequeue(out callback))
                        {
                            return;
                        }
                    }

                    callback.Callback(callback.State);
                }
            }
        }

        private sealed class AsynchronouslyCompletingEofStream : Stream
        {
            private readonly TaskCompletionSource<int> readCompletion = new(
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

            public void CompleteRead()
            {
                readCompletion.TrySetResult(0);
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<int>(readCompletion.Task.WaitAsync(cancellationToken));
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
