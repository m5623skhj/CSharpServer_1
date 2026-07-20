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
        TimeSpan responseTimeout)
    {
        ValidateResponseTimeout(responseTimeout);

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);

        await using var stream = client.GetStream();
        return await SendEchoRequestAsync(stream, message, responseTimeout);
    }

    public string SendEchoRequest(Stream stream, string message)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        stream.Write(packet);

        var responsePayload = ReadResponsePayload(stream);

        return Encoding.UTF8.GetString(responsePayload);
    }

    public async Task<string> SendEchoRequestAsync(Stream stream, string message, TimeSpan responseTimeout)
    {
        ValidateResponseTimeout(responseTimeout);

        using var cancellationTokenSource = new CancellationTokenSource(responseTimeout);

        try
        {
            var payload = Encoding.UTF8.GetBytes(message);
            var packet = PacketEncoder.Encode(payload);

            await stream.WriteAsync(packet, cancellationTokenSource.Token);

            var responsePayload = await ReadResponsePayloadAsync(stream, cancellationTokenSource.Token);

            return Encoding.UTF8.GetString(responsePayload);
        }
        catch (OperationCanceledException exception)
        {
            throw new TimeoutException("Echo response was not received before timeout.", exception);
        }
    }

    private static void ValidateResponseTimeout(TimeSpan responseTimeout)
    {
        if (responseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeout));
        }
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
