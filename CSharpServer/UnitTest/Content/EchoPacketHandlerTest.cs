using CSharpServer.Content;
using CSharpServer.Network;

namespace UnitTest.Content
{
    public class EchoPacketHandlerTest
    {
        [Fact]
        public void Handle_ThrowsArgumentNullException_WhenSenderIsNull()
        {
            var handler = new EchoPacketHandler();

            Assert.Throws<ArgumentNullException>(() =>
                handler.Handle(null!, [0x01]));
        }

        [Fact]
        public void HandleAsync_ThrowsArgumentNullException_WhenSenderIsNull()
        {
            var handler = new EchoPacketHandler();

            void HandleWithNullSender()
            {
                _ = handler.HandleAsync(null!, [0x01], CancellationToken.None);
            }

            Assert.Throws<ArgumentNullException>(HandleWithNullSender);
        }

        [Fact]
        public void Handle_SendsSamePayload()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var sender = new RecordingConnectionSender();
            var handler = new EchoPacketHandler();

            handler.Handle(sender, payload);

            Assert.Same(payload, sender.SentPayload);
        }

        [Fact]
        public void Handle_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            var sender = new RecordingConnectionSender();
            var handler = new EchoPacketHandler();

            var exception = Assert.Throws<ArgumentNullException>(() =>
                handler.Handle(sender, null!));

            Assert.Equal("payload", exception.ParamName);
            Assert.Null(sender.SentPayload);
        }

        [Fact]
        public void HandleAsync_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            var sender = new RecordingConnectionSender();
            var handler = new EchoPacketHandler();

            void HandleNullPayload()
            {
                _ = handler.HandleAsync(sender, null!, CancellationToken.None);
            }

            var exception = Assert.Throws<ArgumentNullException>(HandleNullPayload);

            Assert.Equal("payload", exception.ParamName);
            Assert.Null(sender.SentPayload);
        }

        [Fact]
        public async Task HandleAsync_SendsSamePayloadAndCancellationToken()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var sender = new RecordingConnectionSender();
            using var cancellation = new CancellationTokenSource();
            var handler = new EchoPacketHandler();

            await handler.HandleAsync(sender, payload, cancellation.Token);

            Assert.Same(payload, sender.SentPayload);
            Assert.Equal(cancellation.Token, sender.CancellationToken);
        }

        private sealed class RecordingConnectionSender : IConnectionSender
        {
            public byte[]? SentPayload { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public void Send(byte[] payload)
            {
                SentPayload = payload;
            }

            public ValueTask SendAsync(
                byte[] payload,
                CancellationToken cancellationToken)
            {
                SentPayload = payload;
                CancellationToken = cancellationToken;
                return ValueTask.CompletedTask;
            }
        }
    }
}
