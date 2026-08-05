using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
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
        public void SendEchoRequest_WithStream_AllowsEmptyMessage()
        {
            using var stream = new ScriptedStream(PacketEncoder.Encode([]));
            var client = new EchoClient();

            var response = client.SendEchoRequest(stream, string.Empty);

            Assert.Equal(string.Empty, response);
            Assert.Equal(PacketEncoder.Encode([]), stream.WrittenData);
        }

        [Fact]
        public void SendEchoRequest_WithStream_AllowsMessageAtProtocolLimit()
        {
            var message = new string('a', ProtocolLimits.MaxPayloadLength);
            var encodedPacket = PacketEncoder.Encode(Encoding.UTF8.GetBytes(message));
            using var stream = new ScriptedStream(encodedPacket);
            var client = new EchoClient();

            var response = client.SendEchoRequest(stream, message);

            Assert.Equal(message, response);
            Assert.Equal(encodedPacket, stream.WrittenData);
        }

        [Fact]
        public void SendEchoRequest_ThrowsEndOfStreamException_WhenResponseIsNotReceived()
        {
            using var stream = new ScriptedStream([]);
            var client = new EchoClient();

            Assert.Throws<EndOfStreamException>(() =>
            {
                client.SendEchoRequest(stream, "hello");
            });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(ProtocolLimits.MaxPayloadLength + 1)]
        public void SendEchoRequest_ThrowsInvalidDataException_WhenResponseLengthIsInvalid(
            int responseLength)
        {
            var responseHeader = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(responseHeader, responseLength);
            using var stream = new ScriptedStream(responseHeader);
            var client = new EchoClient();

            Assert.Throws<InvalidDataException>(() =>
                client.SendEchoRequest(stream, "hello"));
            Assert.Equal(
                PacketEncoder.Encode(Encoding.UTF8.GetBytes("hello")),
                stream.WrittenData);
        }

        [Fact]
        public void SendEchoRequest_ThrowsArgumentNullException_WhenStreamIsNull()
        {
            var client = new EchoClient();

            Assert.Throws<ArgumentNullException>(() =>
                client.SendEchoRequest(null!, "hello"));
        }

        [Fact]
        public void SendEchoRequest_ThrowsArgumentNullException_WhenMessageIsNull()
        {
            using var stream = new ScriptedStream([]);
            var client = new EchoClient();

            Assert.Throws<ArgumentNullException>(() =>
                client.SendEchoRequest(stream, null!));
        }

        [Fact]
        public void SendEchoRequest_WithHostAndPort_ThrowsArgumentNullException_WhenHostIsNull()
        {
            var client = new EchoClient();

            Assert.Throws<ArgumentNullException>(() =>
                client.SendEchoRequest(null!, 1, "hello"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void SendEchoRequest_WithHostAndPort_ThrowsArgumentException_WhenHostIsEmpty(
            string host)
        {
            var client = new EchoClient();

            Assert.Throws<ArgumentException>(() =>
                client.SendEchoRequest(host, 1, "hello"));
        }

        [Fact]
        public void SendEchoRequest_WithHostAndPort_ThrowsArgumentNullException_WhenMessageIsNull()
        {
            var client = new EchoClient();

            Assert.Throws<ArgumentNullException>(() =>
                client.SendEchoRequest("127.0.0.1", 1, null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65536)]
        public void SendEchoRequest_WithHostAndPort_ThrowsArgumentOutOfRangeException_WhenPortIsInvalid(
            int port)
        {
            var client = new EchoClient();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                client.SendEchoRequest("127.0.0.1", port, "hello"));
        }

        [Fact]
        public void SendEchoRequest_WithHostAndPort_ThrowsArgumentException_WhenMessageExceedsProtocolLimit()
        {
            var message = new string(
                '\u20AC',
                (ProtocolLimits.MaxPayloadLength / 3) + 1);
            var client = new EchoClient();

            Assert.Throws<ArgumentException>(() =>
                client.SendEchoRequest("127.0.0.1", port: 1, message));
        }

        [Fact]
        public void SendEchoRequest_WithStream_ThrowsArgumentException_WhenMessageExceedsProtocolLimit()
        {
            var message = new string(
                '\u20AC',
                (ProtocolLimits.MaxPayloadLength / 3) + 1);
            using var stream = new ScriptedStream([]);
            var client = new EchoClient();

            var exception = Assert.Throws<ArgumentException>(() =>
                client.SendEchoRequest(stream, message));
            Assert.Equal("message", exception.ParamName);
            Assert.Empty(stream.WrittenData);
        }

        [Fact]
        public async Task SendEchoRequestAsync_ThrowsTimeoutException_WhenRequestDoesNotCompleteBeforeTimeout()
        {
            using var stream = new WaitingReadStream();
            var client = new EchoClient();

            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                client.SendEchoRequestAsync(stream, "hello", TimeSpan.FromMilliseconds(50)));

            Assert.Contains("request", exception.Message);
            Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
            Assert.Equal(
                PacketEncoder.Encode(Encoding.UTF8.GetBytes("hello")),
                stream.WrittenData);
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public async Task SendEchoRequestAsync_PreservesTimeoutException_WhenStreamCloseThrows()
        {
            var stream = new WaitingReadStream(throwOnDispose: true);
            var client = new EchoClient();

            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                client.SendEchoRequestAsync(stream, "hello", TimeSpan.FromMilliseconds(50)));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsAssignableFrom<OperationCanceledException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ThrowsTimeoutException_WhenRequestDoesNotCompleteBeforeTimeout()
        {
            using var stream = new WaitingReadStream();
            var client = new EchoClient();

            Assert.Throws<TimeoutException>(() =>
                client.SendEchoRequest(
                    stream,
                    "hello",
                    TimeSpan.FromMilliseconds(50)));
        }

        [Fact]
        public async Task SendEchoRequestAsync_WithHostAndPort_ThrowsTimeoutException_WhenServerDoesNotRespond()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                var serverTask = AcceptRequestWithoutRespondingAsync(listener);
                var client = new EchoClient();

                await Assert.ThrowsAsync<TimeoutException>(() =>
                    client.SendEchoRequestAsync(
                        "127.0.0.1",
                        port,
                        "hello",
                        TimeSpan.FromMilliseconds(100)));

                await serverTask;
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task SendEchoRequestAsync_WithHostAndPort_PropagatesCancellationDuringConnect()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();
            var client = new EchoClient();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.SendEchoRequestAsync(
                    "127.0.0.1",
                    port: 1,
                    "hello",
                    cancellationTokenSource.Token));
        }

        private static async Task AcceptRequestWithoutRespondingAsync(TcpListener listener)
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            using var stream = serverClient.GetStream();
            var requestBuffer = new byte[sizeof(int) + "hello".Length];

            await stream.ReadExactlyAsync(requestBuffer);

            try
            {
                await stream.ReadExactlyAsync(new byte[1]);
            }
            catch (EndOfStreamException)
            {
            }
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

        private sealed class WaitingReadStream : Stream
        {
            private readonly MemoryStream writeStream = new();
            private readonly bool throwOnDispose;

            public WaitingReadStream(bool throwOnDispose = false)
            {
                this.throwOnDispose = throwOnDispose;
            }

            public byte[] WrittenData => writeStream.ToArray();
            public bool IsDisposed { get; private set; }

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
                throw new NotSupportedException();
            }

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
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
                writeStream.Write(buffer, offset, count);
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                writeStream.Write(buffer.Span);
                return ValueTask.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                    writeStream.Dispose();
                }

                base.Dispose(disposing);
                if (disposing && throwOnDispose)
                {
                    throw new IOException("close failed");
                }
            }
        }
    }
}
