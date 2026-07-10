using System.Net.Sockets;
using System.Text;
using CSharpServer.Packet;

namespace CSharpClient;

internal static class Program
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;
    private const string DefaultMessage = "hello";
    private const int ReceiveBufferSize = 4096;

    public static void Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : DefaultHost;
        var port = args.Length > 1 ? int.Parse(args[1]) : DefaultPort;
        var message = args.Length > 2 ? args[2] : DefaultMessage;

        var response = SendEchoRequest(host, port, message);

        Console.WriteLine(response);
    }

    private static string SendEchoRequest(string host, int port, string message)
    {
        using var client = new TcpClient();
        client.Connect(host, port);

        using var stream = client.GetStream();
        var payload = Encoding.UTF8.GetBytes(message);
        var packet = PacketEncoder.Encode(payload);

        stream.Write(packet);

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
                return Encoding.UTF8.GetString(responsePayload);
            }
        }
    }
}
