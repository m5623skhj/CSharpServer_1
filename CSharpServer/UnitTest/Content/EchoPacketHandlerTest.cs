using CSharpServer.Content;

namespace UnitTest.Content
{
    public class EchoPacketHandlerTest
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPacketSenderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EchoPacketHandler(null!));
        }

        [Fact]
        public void Handle_SendsSamePayload()
        {
            var payload = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var sentPayloads = new List<byte[]>();
            var handler = new EchoPacketHandler(sentPayloads.Add);

            handler.Handle(payload);

            var sentPayload = Assert.Single(sentPayloads);
            Assert.Equal(payload, sentPayload);
        }

        [Fact]
        public void Handle_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            var sentPayloads = new List<byte[]>();
            var handler = new EchoPacketHandler(sentPayloads.Add);

            var exception = Assert.Throws<ArgumentNullException>(() =>
                handler.Handle(null!));

            Assert.Equal("payload", exception.ParamName);
            Assert.Empty(sentPayloads);
        }

        [Fact]
        public void HandleAsync_ThrowsArgumentNullException_WhenPayloadIsNull()
        {
            var sentPayloads = new List<byte[]>();
            var handler = new EchoPacketHandler(sentPayloads.Add);

            void HandleNullPayload()
            {
                _ = handler.HandleAsync(null!, CancellationToken.None);
            }

            var exception = Assert.Throws<ArgumentNullException>(HandleNullPayload);

            Assert.Equal("payload", exception.ParamName);
            Assert.Empty(sentPayloads);
        }
    }
}
