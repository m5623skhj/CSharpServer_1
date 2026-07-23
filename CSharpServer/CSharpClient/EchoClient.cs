using System.Net.Sockets;
using System.Text;
using CSharpServer.Packet;

namespace CSharpClient;

public sealed class EchoClient
{
    private const int ReceiveBufferSize = 4096;
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(5);

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

    public async Task<string> SendEchoRequestAsync(Stream stream, string message, TimeSpan requestTimeout)
    {
        ValidateRequestTimeout(requestTimeout);

        using var cancellationTokenSource = new CancellationTokenSource(requestTimeout);

        try
        {
            return await SendEchoRequestAsyncCore(
                stream,
                message,
                cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (cancellationTokenSource.IsCancellationRequested)
        {
            stream.Close();
            throw CreateTimeoutException(exception);
        }
    }

    private static async Task<string> SendEchoRequestAsyncCore(
        Stream stream,
        string message,
        CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        await stream.WriteAsync(packet, cancellationToken).ConfigureAwait(false);

        var responsePayload = await ReadResponsePayloadAsync(stream, cancellationToken)
            .ConfigureAwait(false);

        return Encoding.UTF8.GetString(responsePayload);
    }

    private static void ValidateRequestTimeout(TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }
    }

    private static TimeoutException CreateTimeoutException(OperationCanceledException exception)
    {
        return new TimeoutException("Echo request did not complete before timeout.", exception);
    }

    private static async Task<byte[]> ReadResponsePayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var packetBuffer = new PacketBuffer();
        var receiveBuffer = new byte[ReceiveBufferSize];

        while (true)
        {
            var readCount = await stream.ReadAsync(receiveBuffer, cancellationToken)
                .ConfigureAwait(false);
            if (readCount == 0)
            {
                throw new EndOfStreamException(
                    "Connection closed before echo response was received.");
            }

            packetBuffer.Append(receiveBuffer.AsSpan(0, readCount));
            if (packetBuffer.TryReadPacket(out var responsePayload) && responsePayload is not null)
            {
                return responsePayload;
            }
        }
    }
}
