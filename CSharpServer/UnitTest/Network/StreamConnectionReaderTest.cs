using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionReaderTest
    {
        [Fact]
        public void ReadOnce_InvokesDataHandler_WithReadData()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            using var stream = new MemoryStream(data);
            var receivedData = new List<byte[]>();
            var reader = new StreamConnectionReader(stream, inBufferSize: 8, receivedData.Add);

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
            var reader = new StreamConnectionReader(stream, inBufferSize: 8, receivedData.Add);

            var result = reader.ReadOnce();

            Assert.False(result);
            Assert.Empty(receivedData);
        }
    }
}
