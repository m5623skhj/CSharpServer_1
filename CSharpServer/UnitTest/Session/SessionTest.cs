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
        public async Task Receive_SerializesPacketHandlers_WhenCalledConcurrently()
        {
            var handler = new ConcurrentPacketHandler();
            var session = new NetworkSession(handler.Handle);
            var firstReceive = Task.Run(() => session.Receive(PacketEncoder.Encode([0x01])));

            Assert.True(handler.FirstHandlerEntered.Wait(TimeSpan.FromSeconds(1)));

            var secondReceive = Task.Run(() =>
            {
                handler.SecondReceiveRequested.TrySetResult();
                session.Receive(PacketEncoder.Encode([0x02]));
            });
            await handler.SecondReceiveRequested.Task.WaitAsync(TimeSpan.FromSeconds(1));
            handler.AllowFirstHandlerToComplete.Set();
            await Task.WhenAll(firstReceive, secondReceive);

            Assert.False(handler.HadOverlappingHandlers);
        }

        private sealed class ConcurrentPacketHandler
        {
            private int activeHandlerCount;
            private int handlerInvocationCount;

            public ManualResetEventSlim FirstHandlerEntered { get; } = new();
            public TaskCompletionSource SecondReceiveRequested { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public ManualResetEventSlim AllowFirstHandlerToComplete { get; } = new();
            public bool HadOverlappingHandlers { get; private set; }

            public void Handle(byte[] payload)
            {
                if (Interlocked.Increment(ref activeHandlerCount) > 1)
                {
                    HadOverlappingHandlers = true;
                }

                try
                {
                    if (Interlocked.Increment(ref handlerInvocationCount) == 1)
                    {
                        FirstHandlerEntered.Set();
                        SecondReceiveRequested.Task.GetAwaiter().GetResult();
                        AllowFirstHandlerToComplete.Wait();
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
