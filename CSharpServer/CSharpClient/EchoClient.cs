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

    public string SendEchoRequest(Stream stream, string message)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        stream.Write(packet);

        var responsePayload = ReadResponsePayload(stream);

        return Encoding.UTF8.GetString(responsePayload);
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
}
