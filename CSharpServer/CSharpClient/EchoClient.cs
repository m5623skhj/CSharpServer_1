using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using CSharpServer.Packet;

namespace CSharpClient;

public sealed class EchoClient
{
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxTimerDelay =
        TimeSpan.FromMilliseconds(uint.MaxValue - 1);
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public string SendEchoRequest(string host, int port, string message)
    {
        return SendEchoRequest(host, port, message, DefaultRequestTimeout);
    }

    public string SendEchoRequest(
        string host,
        int port,
        string message,
        TimeSpan requestTimeout)
    {
        return SendEchoRequestAsync(host, port, message, requestTimeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public string SendEchoRequest(
        string host,
        int port,
        string message,
        CancellationToken cancellationToken)
    {
        return SendEchoRequestAsync(host, port, message, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<string> SendEchoRequestAsync(
        string host,
        int port,
        string message,
        TimeSpan requestTimeout)
    {
        ValidateRequestTimeout(requestTimeout);

        using var cancellationTokenSource = new CancellationTokenSource(requestTimeout);

        try
        {
            return await SendEchoRequestAsync(
                host,
                port,
                message,
                cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (cancellationTokenSource.IsCancellationRequested)
        {
            throw CreateTimeoutException(exception);
        }
    }

    public async Task<string> SendEchoRequestAsync(
        string host,
        int port,
        string message,
        CancellationToken cancellationToken)
    {
        ValidateHostRequest(host, port, message);

        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        await using var stream = client.GetStream();
        return await SendEchoRequestAsyncCore(stream, message, cancellationToken)
            .ConfigureAwait(false);
    }

    public string SendEchoRequest(Stream stream, string message)
    {
        return SendEchoRequest(stream, message, DefaultRequestTimeout);
    }

    public string SendEchoRequest(Stream stream, string message, TimeSpan requestTimeout)
    {
        return SendEchoRequestAsync(stream, message, requestTimeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public string SendEchoRequest(
        Stream stream,
        string message,
        CancellationToken cancellationToken)
    {
        return SendEchoRequestAsync(stream, message, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<string> SendEchoRequestAsync(Stream stream, string message, TimeSpan requestTimeout)
    {
        ValidateRequestTimeout(requestTimeout);

        using var cancellationTokenSource = new CancellationTokenSource(requestTimeout);

        return await SendEchoRequestAsync(
            stream,
            message,
            cancellationTokenSource.Token,
            translateCancellationToTimeout: true).ConfigureAwait(false);
    }

    public async Task<string> SendEchoRequestAsync(
        Stream stream,
        string message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(message);
        ValidateMessageSize(message);
        cancellationToken.ThrowIfCancellationRequested();

        return await SendEchoRequestAsync(
            stream,
            message,
            cancellationToken,
            translateCancellationToTimeout: false).ConfigureAwait(false);
    }

    private static async Task<string> SendEchoRequestAsync(
        Stream stream,
        string message,
        CancellationToken cancellationToken,
        bool translateCancellationToTimeout)
    {
        try
        {
            return await SendEchoRequestAsyncCore(
                stream,
                message,
                cancellationToken).ConfigureAwait(false);
        }
        catch (EndOfStreamException exception)
        {
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new EndOfStreamException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }

            throw;
        }
        catch (InvalidDataException exception)
        {
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new InvalidDataException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }

            throw;
        }
        catch (IOException exception)
        {
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new IOException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }

            throw;
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                var combinedException = new AggregateException(
                    exception,
                    closeException);
                if (translateCancellationToTimeout)
                {
                    throw CreateTimeoutException(combinedException);
                }

                throw new OperationCanceledException(
                    exception.Message,
                    combinedException,
                    cancellationToken);
            }

            if (translateCancellationToTimeout)
            {
                throw CreateTimeoutException(exception);
            }

            throw;
        }
    }

    private static async Task<string> SendEchoRequestAsyncCore(
        Stream stream,
        string message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(message);
        ValidateMessageSize(message);

        var payload = StrictUtf8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        await stream.WriteAsync(packet, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        var responsePayload = await ReadResponsePayloadAsync(stream, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return StrictUtf8.GetString(responsePayload);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                "Echo response payload is not valid UTF-8.",
                exception);
        }
    }

    private static void ValidateRequestTimeout(TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero || requestTimeout > MaxTimerDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }
    }

    private static void ValidateHostRequest(string host, int port, string message)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be empty.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        ValidateMessageSize(message);
    }

    private static void ValidateMessageSize(string message)
    {
        int messageByteCount;
        try
        {
            messageByteCount = StrictUtf8.GetByteCount(message);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ArgumentException(
                "Message must be valid UTF-8 text.",
                nameof(message),
                exception);
        }

        if (messageByteCount <= ProtocolLimits.MaxPayloadLength)
        {
            return;
        }

        throw new ArgumentException(
            $"Message cannot exceed {ProtocolLimits.MaxPayloadLength} UTF-8 bytes.",
            nameof(message));
    }

    private static TimeoutException CreateTimeoutException(Exception exception)
    {
        return new TimeoutException("Echo request did not complete before timeout.", exception);
    }

    private static async Task<byte[]> ReadResponsePayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var packetBuffer = new PacketBuffer();
        var responseHeader = new byte[sizeof(int)];
        await ReadExactlyAsync(stream, responseHeader, cancellationToken)
            .ConfigureAwait(false);
        packetBuffer.Append(responseHeader);

        if (packetBuffer.TryReadPacket(out var emptyPayload) && emptyPayload is not null)
        {
            return emptyPayload;
        }

        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(responseHeader);
        var payload = new byte[payloadLength];
        await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        packetBuffer.Append(payload);

        if (!packetBuffer.TryReadPacket(out var responsePayload) || responsePayload is null)
        {
            throw new InvalidOperationException(
                "Complete echo response packet could not be decoded.");
        }

        return responsePayload;
    }

    private static async Task ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            var readCount = await stream.ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
            if (readCount == 0)
            {
                throw new EndOfStreamException(
                    "Connection closed before echo response was received.");
            }

            buffer = buffer[readCount..];
        }
    }
}
