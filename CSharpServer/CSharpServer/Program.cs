using System.Net;
using CSharpServer.Network;

namespace CSharpServer;

internal static class Program
{
    private const int DefaultPort = 5000;
    private const int DefaultBufferSize = 4096;
    private const int DefaultClientCount = 1;

    public static void Main(string[] args)
    {
        var port = args.Length > 0 ? int.Parse(args[0]) : DefaultPort;
        var clientCount = args.Length > 1 ? int.Parse(args[1]) : DefaultClientCount;

        using var server = new EchoTcpServer(IPAddress.Loopback, port, DefaultBufferSize);
        server.Start();

        Console.WriteLine($"CSharpServer listening on 127.0.0.1:{server.Port}");
        server.AcceptAndHandle(clientCount);
    }
}
