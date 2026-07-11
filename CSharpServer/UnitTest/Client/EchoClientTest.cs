using System.Text;
using CSharpClient;
using CSharpServer.Packet;

namespace UnitTest.Client
{
    public class EchoClientTest
    {
        [Fact]
        public void SendEchoRequest_WritesEncodedRequestAndReturnsDecodedResponse()
        {
            var requestMessage = "hello";
            var responseMessage = "world";
            using var stream = new ScriptedStream(PacketEncoder.Encode(Encoding.UTF8.GetBytes(responseMessage)));
            var client = new EchoClient();

            var response = client.SendEchoRequest(stream, requestMessage);

            Assert.Equal(responseMessage, response);
            Assert.Equal(
                PacketEncoder.Encode(Encoding.UTF8.GetBytes(requestMessage)),
                stream.WrittenData);
        }

        [Fact]
        public void SendEchoRequest_ThrowsInvalidOperationException_WhenResponseIsNotReceived()
        {
            using var stream = new ScriptedStream([]);
            var client = new EchoClient();

            Assert.Throws<InvalidOperationException>(() =>
            {
                client.SendEchoRequest(stream, "hello");
            });
        }

        private sealed class ScriptedStream : Stream
        {
            private readonly MemoryStream readStream;
            private readonly MemoryStream writeStream = new();

            public ScriptedStream(byte[] readData)
            {
                readStream = new MemoryStream(readData);
            }

            public byte[] WrittenData => writeStream.ToArray();

            public override bool CanRead => true;
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

            public override int Read(byte[] buffer, int offset, int count)
            {
                return readStream.Read(buffer, offset, count);
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
                writeStream.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    readStream.Dispose();
                    writeStream.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
