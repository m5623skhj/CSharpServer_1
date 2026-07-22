using System.Net;
using CSharpServer.Network;

namespace CSharpServer;

public sealed class ServerApplication
{
    private const int DefaultPort = 5000;
    private const int DefaultBufferSize = 4096;

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var port = args.Length > 0 ? int.Parse(args[0]) : DefaultPort;

        using var server = new EchoTcpServer(IPAddress.Loopback, port, DefaultBufferSize);
        server.Start();

        Console.WriteLine($"CSharpServer listening on 127.0.0.1:{server.Port}");
        await server.AcceptAndHandleConcurrently(cancellationToken);
    }
}
