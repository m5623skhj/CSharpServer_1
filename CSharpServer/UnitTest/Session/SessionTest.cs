using CSharpServer.Packet;
using NetworkSession = CSharpServer.Network.Session;

namespace UnitTest.Session
{
    public class SessionTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPacketHandlerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NetworkSession(null!));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPacketSenderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NetworkSession(_ => { }, null!));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenAsyncPacketHandlerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NetworkSession(
                    _ => { },
                    _ => { },
                    null!,
                    (_, _) => ValueTask.CompletedTask));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenAsyncPacketSenderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NetworkSession(
                    _ => { },
                    _ => { },
                    (_, _) => ValueTask.CompletedTask,
                    null!));
        }

        [Fact]
        public void Receive_ThrowsArgumentNullException_WhenDataIsNull()
        {
            var session = new NetworkSession(_ => { });

            Assert.Throws<ArgumentNullException>(() =>
                session.Receive(null!));
        }

        [Fact]
        public void Receive_InvokesPacketHandler_WhenCompletePacketIsReceived()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));

            session.Receive(PacketEncoder.Encode(payload));

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Receive_DoesNotInvokePacketHandler_UntilPacketIsComplete()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));

            session.Receive([0x05, 0x00]);

            Assert.Empty(receivedPackets);

            session.Receive([0x00, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F]);

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public void Receive_InvokesPacketHandlerInOrder_WhenMultiplePacketsAreReceived()
        {
            var firstPayload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var secondPayload = new byte[] { 0x77, 0x6F, 0x72, 0x6C, 0x64 };
            var receivedPackets = new List<byte[]>();
            var session = new NetworkSession(packet => receivedPackets.Add(packet));
            var receivedData = PacketEncoder.Encode(firstPayload)
                .Concat(PacketEncoder.Encode(secondPayload))
                .ToArray();

            session.Receive(receivedData);

            Assert.Collection(
                receivedPackets,
                packet => Assert.Equal(firstPayload, packet),
                packet => Assert.Equal(secondPayload, packet));
        }

        [Fact]
        public void Receive_RejectsFurtherData_AfterInvalidPacketLength()
        {
            var session = new NetworkSession(_ => { });

            Assert.Throws<InvalidDataException>(() =>
                session.Receive([0xFF, 0xFF, 0xFF, 0xFF]));

            Assert.Equal(1, session.AvailableReceiveSlotCount);
            Assert.Throws<ObjectDisposedException>(() =>
                session.Receive(PacketEncoder.Encode([0x01])));
            Assert.Equal(1, session.AvailableReceiveSlotCount);
        }

        [Fact]
        public void Send_InvokesPacketSender_WithEncodedPacket()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var sentPackets = new List<byte[]>();
            var session = new NetworkSession(_ => { }, packet => sentPackets.Add(packet));

            session.Send(payload);

            var sentPacket = Assert.Single(sentPackets);
            Assert.Equal(PacketEncoder.Encode(payload), sentPacket);
        }

        [Fact]
        public void Send_CanBeReceivedByAnotherSession()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var receivedPackets = new List<byte[]>();
            var receiver = new NetworkSession(packet => receivedPackets.Add(packet));
            var sender = new NetworkSession(_ => { }, receiver.Receive);

            sender.Send(payload);

            var receivedPacket = Assert.Single(receivedPackets);
            Assert.Equal(payload, receivedPacket);
        }

        [Fact]
        public async Task ReceiveAsync_SerializesPacketHandlers_WhenCalledConcurrently()
        {
            var handler = new ConcurrentAsyncPacketHandler();
            var session = new NetworkSession(
                _ => { },
                _ => { },
                handler.HandleAsync,
                (_, _) => ValueTask.CompletedTask);
            var firstReceive = session.ReceiveAsync(
                PacketEncoder.Encode([0x01]),
                CancellationToken.None).AsTask();

            await handler.FirstHandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, session.AvailableReceiveSlotCount);

            var secondReceive = session.ReceiveAsync(
                PacketEncoder.Encode([0x02]),
                CancellationToken.None).AsTask();
            Assert.False(secondReceive.IsCompleted);
            handler.AllowFirstHandlerToComplete.TrySetResult();
            await Task.WhenAll(firstReceive, secondReceive);

            Assert.False(handler.HadOverlappingHandlers);
            Assert.Equal(1, session.AvailableReceiveSlotCount);
        }

        [Fact]
        public async Task ReceiveAsync_InvokesHandlerWithPayloadAndCancellationToken()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var cancellation = new CancellationTokenSource();
            byte[]? receivedPayload = null;
            var receivedCancellationToken = CancellationToken.None;
            var session = new NetworkSession(
                _ => { },
                _ => { },
                (packet, cancellationToken) =>
                {
                    receivedPayload = packet;
                    receivedCancellationToken = cancellationToken;
                    return ValueTask.CompletedTask;
                },
                (_, _) => ValueTask.CompletedTask);

            await session.ReceiveAsync(
                PacketEncoder.Encode(payload),
                cancellation.Token);

            Assert.Equal(payload, receivedPayload);
            Assert.Equal(cancellation.Token, receivedCancellationToken);
        }

        [Fact]
        public async Task SendAsync_InvokesSenderWithEncodedPacketAndCancellationToken()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            using var cancellation = new CancellationTokenSource();
            byte[]? sentPacket = null;
            var sentCancellationToken = CancellationToken.None;
            var session = new NetworkSession(
                _ => { },
                _ => { },
                (_, _) => ValueTask.CompletedTask,
                (packet, cancellationToken) =>
                {
                    sentPacket = packet.ToArray();
                    sentCancellationToken = cancellationToken;
                    return ValueTask.CompletedTask;
                });

            await session.SendAsync(payload, cancellation.Token);

            Assert.Equal(PacketEncoder.Encode(payload), sentPacket);
            Assert.Equal(cancellation.Token, sentCancellationToken);
        }

        [Fact]
        public async Task ReceiveAsync_DoesNotBufferData_WhenCanceledWhileWaitingForReceiveSlot()
        {
            var firstHandlerEntered = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowFirstHandlerToComplete = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var handledPayloads = new List<byte[]>();
            var handlerInvocationCount = 0;
            var session = new NetworkSession(
                _ => { },
                _ => { },
                async (payload, cancellationToken) =>
                {
                    handledPayloads.Add(payload);
                    if (Interlocked.Increment(ref handlerInvocationCount) == 1)
                    {
                        firstHandlerEntered.TrySetResult();
                        await allowFirstHandlerToComplete.Task.WaitAsync(cancellationToken);
                    }
                },
                (_, _) => ValueTask.CompletedTask);
            var firstReceive = session.ReceiveAsync(
                PacketEncoder.Encode([0x01]),
                CancellationToken.None).AsTask();

            await firstHandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            using var secondCancellation = new CancellationTokenSource();
            var secondReceive = session.ReceiveAsync(
                PacketEncoder.Encode([0x02]),
                secondCancellation.Token).AsTask();
            await secondCancellation.CancelAsync();

            try
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondReceive);
            }
            finally
            {
                allowFirstHandlerToComplete.TrySetResult();
                await firstReceive.WaitAsync(TimeSpan.FromSeconds(1));
            }

            await session.ReceiveAsync(ReadOnlyMemory<byte>.Empty, CancellationToken.None);

            var handledPayload = Assert.Single(handledPayloads);
            Assert.Equal(new byte[] { 0x01 }, handledPayload);
            Assert.Equal(1, session.AvailableReceiveSlotCount);
        }

        [Fact]
        public async Task ReceiveAsync_ReleasesReceiveSlot_WhenHandlerThrows()
        {
            var expectedException = new InvalidOperationException("Handler failed.");
            var handledPayloads = new List<byte[]>();
            var handlerInvocationCount = 0;
            var session = new NetworkSession(
                _ => { },
                _ => { },
                (payload, _) =>
                {
                    if (Interlocked.Increment(ref handlerInvocationCount) == 1)
                    {
                        return ValueTask.FromException(expectedException);
                    }

                    handledPayloads.Add(payload);
                    return ValueTask.CompletedTask;
                },
                (_, _) => ValueTask.CompletedTask);

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                session.ReceiveAsync(
                    PacketEncoder.Encode([0x01]),
                    CancellationToken.None).AsTask());

            Assert.Same(expectedException, actualException);
            Assert.Equal(1, session.AvailableReceiveSlotCount);

            await session.ReceiveAsync(
                PacketEncoder.Encode([0x02]),
                CancellationToken.None);

            var handledPayload = Assert.Single(handledPayloads);
            Assert.Equal(new byte[] { 0x02 }, handledPayload);
            Assert.Equal(1, session.AvailableReceiveSlotCount);
        }

        [Fact]
        public async Task ReceiveAsync_RejectsFurtherData_AfterHandlerReportsInvalidData()
        {
            var expectedException = new InvalidDataException("Invalid packet payload.");
            var handlerInvocationCount = 0;
            var session = new NetworkSession(
                _ => { },
                _ => { },
                (_, _) =>
                {
                    Interlocked.Increment(ref handlerInvocationCount);
                    return ValueTask.FromException(expectedException);
                },
                (_, _) => ValueTask.CompletedTask);

            var actualException = await Assert.ThrowsAsync<InvalidDataException>(() =>
                session.ReceiveAsync(
                    PacketEncoder.Encode([0x01]),
                    CancellationToken.None).AsTask());

            Assert.Same(expectedException, actualException);
            Assert.Equal(1, session.AvailableReceiveSlotCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                session.ReceiveAsync(
                    PacketEncoder.Encode([0x02]),
                    CancellationToken.None).AsTask());
            Assert.Equal(1, handlerInvocationCount);
            Assert.Equal(1, session.AvailableReceiveSlotCount);
        }

        [Fact]
        public async Task ReceiveAsync_DoesNotCaptureSynchronizationContext()
        {
            var handlerCompletion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var session = new NetworkSession(
                _ => { },
                _ => { },
                (_, _) => new ValueTask(handlerCompletion.Task),
                (_, _) => ValueTask.CompletedTask);
            var synchronizationContext = new QueueingSynchronizationContext();
            var receiveCompletion = new TaskCompletionSource<Exception?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var synchronousWaitStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var receiveThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    var receiveTask = session.ReceiveAsync(
                            PacketEncoder.Encode([0x01]),
                            CancellationToken.None)
                        .AsTask();
                    synchronousWaitStarted.TrySetResult();
                    receiveTask.GetAwaiter().GetResult();
                    receiveCompletion.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    receiveCompletion.TrySetResult(exception);
                }
            })
            {
                IsBackground = true
            };

            receiveThread.Start();
            await synchronousWaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            handlerCompletion.TrySetResult();
            var firstCompletion = await Task.WhenAny(
                    receiveCompletion.Task,
                    synchronizationContext.ContinuationPosted.Task)
                .WaitAsync(TimeSpan.FromSeconds(1));

            try
            {
                Assert.Same(receiveCompletion.Task, firstCompletion);
                Assert.Null(await receiveCompletion.Task);
                Assert.Equal(1, session.AvailableReceiveSlotCount);
            }
            finally
            {
                synchronizationContext.RunAll();
                Assert.True(receiveThread.Join(TimeSpan.FromSeconds(1)));
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

        private sealed class ConcurrentAsyncPacketHandler
        {
            private int activeHandlerCount;
            private int handlerInvocationCount;

            public TaskCompletionSource FirstHandlerEntered { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource AllowFirstHandlerToComplete { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public bool HadOverlappingHandlers { get; private set; }

            public async ValueTask HandleAsync(
                byte[] payload,
                CancellationToken cancellationToken)
            {
                if (Interlocked.Increment(ref activeHandlerCount) > 1)
                {
                    HadOverlappingHandlers = true;
                }

                try
                {
                    if (Interlocked.Increment(ref handlerInvocationCount) == 1)
                    {
                        FirstHandlerEntered.TrySetResult();
                        await AllowFirstHandlerToComplete.Task.WaitAsync(cancellationToken);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeHandlerCount);
                }
            }
        }
    }
}
