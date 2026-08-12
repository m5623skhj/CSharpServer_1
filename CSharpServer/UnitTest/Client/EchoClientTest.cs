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
            Assert.False(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_DoesNotConsumeNextResponse_WhenResponsesShareReadBuffer()
        {
            var firstResponse = PacketEncoder.Encode(Encoding.UTF8.GetBytes("first"));
            var secondResponse = PacketEncoder.Encode(Encoding.UTF8.GetBytes("second"));
            using var stream = new ScriptedStream(firstResponse.Concat(secondResponse).ToArray());
            var client = new EchoClient();

            var firstResult = client.SendEchoRequest(stream, "first request");

            Assert.Equal("first", firstResult);
            Assert.Equal(secondResponse.Length, stream.RemainingReadByteCount);

            var secondResult = client.SendEchoRequest(stream, "second request");

            Assert.Equal("second", secondResult);
            Assert.Equal(0, stream.RemainingReadByteCount);
            Assert.False(stream.IsDisposed);
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
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_PreservesEndOfStreamException_WhenStreamCloseThrows()
        {
            var stream = new ScriptedStream([], throwOnDispose: true);
            var client = new EchoClient();

            var exception = Assert.Throws<EndOfStreamException>(() =>
                client.SendEchoRequest(stream, "hello"));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsType<EndOfStreamException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ClosesStream_WhenResponseEndsAfterPartialHeader()
        {
            using var stream = new ScriptedStream([0x01, 0x00]);
            var client = new EchoClient();

            Assert.Throws<EndOfStreamException>(() =>
                client.SendEchoRequest(stream, "hello"));

            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ClosesStream_WhenResponseEndsAfterPartialPayload()
        {
            using var stream = new ScriptedStream([
                0x05, 0x00, 0x00, 0x00,
                0x68, 0x65
            ]);
            var client = new EchoClient();

            Assert.Throws<EndOfStreamException>(() =>
                client.SendEchoRequest(stream, "hello"));

            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ReturnsResponse_WhenResponseIsSplitAcrossReads()
        {
            var encodedResponse = PacketEncoder.Encode(Encoding.UTF8.GetBytes("world"));
            using var stream = new ScriptedStream(encodedResponse, maxReadSize: 1);
            var client = new EchoClient();

            var response = client.SendEchoRequest(stream, "hello");

            Assert.Equal("world", response);
            Assert.Equal(0, stream.RemainingReadByteCount);
            Assert.False(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ClosesStream_WhenResponseReadThrowsIOException()
        {
            var expectedException = new IOException("read failed");
            using var stream = new ScriptedStream(
                [],
                readException: expectedException);
            var client = new EchoClient();

            var exception = Assert.Throws<IOException>(() =>
                client.SendEchoRequest(stream, "hello"));

            Assert.Same(expectedException, exception);
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_PreservesIOException_WhenStreamCloseThrows()
        {
            var expectedException = new IOException("read failed");
            var stream = new ScriptedStream(
                [],
                throwOnDispose: true,
                readException: expectedException);
            var client = new EchoClient();

            var exception = Assert.Throws<IOException>(() =>
                client.SendEchoRequest(stream, "hello"));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.Same(expectedException, item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ClosesStream_WhenRequestWriteThrowsIOException()
        {
            var expectedException = new IOException("write failed");
            using var stream = new ScriptedStream(
                [],
                writeException: expectedException);
            var client = new EchoClient();

            var exception = Assert.Throws<IOException>(() =>
                client.SendEchoRequest(stream, "hello"));

            Assert.Same(expectedException, exception);
            Assert.Empty(stream.WrittenData);
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_ThrowsInvalidDataException_WhenResponseIsNotValidUtf8()
        {
            using var stream = new ScriptedStream(PacketEncoder.Encode([0xC3, 0x28]));
            var client = new EchoClient();

            var exception = Assert.Throws<InvalidDataException>(() =>
                client.SendEchoRequest(stream, "hello"));

            Assert.IsType<DecoderFallbackException>(exception.InnerException);
            Assert.True(stream.IsDisposed);
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
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void SendEchoRequest_PreservesInvalidDataException_WhenStreamCloseThrows()
        {
            var responseHeader = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(responseHeader, -1);
            var stream = new ScriptedStream(responseHeader, throwOnDispose: true);
            var client = new EchoClient();

            var exception = Assert.Throws<InvalidDataException>(() =>
                client.SendEchoRequest(stream, "hello"));

            var innerException = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(
                innerException.InnerExceptions,
                item => Assert.IsType<InvalidDataException>(item),
                item => Assert.IsType<IOException>(item));
            Assert.True(stream.IsDisposed);
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
        public void SendEchoRequest_WithStream_ThrowsArgumentException_WhenMessageIsNotValidUtf16()
        {
            using var stream = new ScriptedStream(PacketEncoder.Encode([]));
            var client = new EchoClient();

            var exception = Assert.Throws<ArgumentException>(() =>
                client.SendEchoRequest(stream, "\uD800"));

            Assert.Equal("message", exception.ParamName);
            Assert.IsType<EncoderFallbackException>(exception.InnerException);
            Assert.Empty(stream.WrittenData);
            Assert.False(stream.IsDisposed);
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
        public async Task SendEchoRequestAsync_ThrowsArgumentOutOfRangeException_WhenRequestTimeoutExceedsTimerLimit()
        {
            using var stream = new ScriptedStream([]);
            var client = new EchoClient();

            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                client.SendEchoRequestAsync(stream, "hello", TimeSpan.MaxValue));

            Assert.Equal("requestTimeout", exception.ParamName);
            Assert.Empty(stream.WrittenData);
        }

        [Fact]
        public async Task SendEchoRequestAsync_AllowsRequestTimeoutAtTimerLimit()
        {
            var responseMessage = "world";
            using var stream = new ScriptedStream(
                PacketEncoder.Encode(Encoding.UTF8.GetBytes(responseMessage)));
            var client = new EchoClient();

            var response = await client.SendEchoRequestAsync(
                stream,
                "hello",
                TimeSpan.FromMilliseconds(uint.MaxValue - 1));

            Assert.Equal(responseMessage, response);
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
            private readonly bool throwOnDispose;
            private readonly IOException? readException;
            private readonly IOException? writeException;
            private readonly int? maxReadSize;

            public ScriptedStream(
                byte[] readData,
                bool throwOnDispose = false,
                IOException? readException = null,
                IOException? writeException = null,
                int? maxReadSize = null)
            {
                readStream = new MemoryStream(readData);
                this.throwOnDispose = throwOnDispose;
                this.readException = readException;
                this.writeException = writeException;
                this.maxReadSize = maxReadSize;
            }

            public byte[] WrittenData => writeStream.ToArray();
            public long RemainingReadByteCount => readStream.Length - readStream.Position;
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
                if (readException is not null)
                {
                    throw readException;
                }

                return readStream.Read(
                    buffer,
                    offset,
                    Math.Min(count, maxReadSize ?? count));
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
                if (writeException is not null)
                {
                    throw writeException;
                }

                writeStream.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IsDisposed = true;
                    readStream.Dispose();
                    writeStream.Dispose();
                }

                base.Dispose(disposing);
                if (disposing && throwOnDispose)
                {
                    throw new IOException("close failed");
                }
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
