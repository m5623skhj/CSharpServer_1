using CSharpServer.Packet;
using NetworkSession = CSharpServer.Network.Session;

namespace UnitTest.Session
{
    public class SessionTest
    {
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
