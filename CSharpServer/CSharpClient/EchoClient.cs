using System.Net.Sockets;
using System.Text;
using CSharpServer.Packet;

namespace CSharpClient;

public sealed class EchoClient
{
    private const int ReceiveBufferSize = 4096;

    public string SendEchoRequest(string host, int port, string message)
    {
        using var client = new TcpClient();
        client.Connect(host, port);

        using var stream = client.GetStream();
        return SendEchoRequest(stream, message);
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
                cancellationTokenSource.Token);
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
        await client.ConnectAsync(host, port, cancellationToken);

        await using var stream = client.GetStream();
        return await SendEchoRequestAsyncCore(stream, message, cancellationToken);
    }

    public string SendEchoRequest(Stream stream, string message)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        stream.Write(packet);

        var responsePayload = ReadResponsePayload(stream);

        return Encoding.UTF8.GetString(responsePayload);
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
                cancellationTokenSource.Token);
        }
        catch (OperationCanceledException exception)
            when (cancellationTokenSource.IsCancellationRequested)
        {
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

        await stream.WriteAsync(packet, cancellationToken);

        var responsePayload = await ReadResponsePayloadAsync(stream, cancellationToken);

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

    private static byte[] ReadResponsePayload(Stream stream)
    {
        var packetBuffer = new PacketBuffer();
        var receiveBuffer = new byte[ReceiveBufferSize];

        while (true)
        {
            var readCount = stream.Read(receiveBuffer);
            if (readCount == 0)
            {
                throw new InvalidOperationException("Connection closed before echo response was received.");
            }

            packetBuffer.Append(receiveBuffer[..readCount]);
            if (packetBuffer.TryReadPacket(out var responsePayload) && responsePayload is not null)
            {
                return responsePayload;
            }
        }
    }

    private static async Task<byte[]> ReadResponsePayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var packetBuffer = new PacketBuffer();
        var receiveBuffer = new byte[ReceiveBufferSize];

        while (true)
        {
            var readCount = await stream.ReadAsync(receiveBuffer, cancellationToken);
            if (readCount == 0)
            {
                throw new InvalidOperationException("Connection closed before echo response was received.");
            }

            packetBuffer.Append(receiveBuffer[..readCount]);
            if (packetBuffer.TryReadPacket(out var responsePayload) && responsePayload is not null)
            {
                return responsePayload;
            }
        }
    }
}
