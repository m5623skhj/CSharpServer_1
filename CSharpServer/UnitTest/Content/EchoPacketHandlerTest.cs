using CSharpServer.Content;

namespace UnitTest.Content
{
    public class EchoPacketHandlerTest
    {
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
    }
}
