using System.Net;
using CSharpServer.Network;

namespace CSharpServer;

public sealed class ServerApplication
{
    private const int DefaultBufferSize = 4096;

    public async Task RunAsync(ServerOptions options, CancellationToken cancellationToken)
    {
        using var server = new EchoTcpServer(IPAddress.Loopback, options.Port, DefaultBufferSize);
        server.Start();

        Console.WriteLine($"CSharpServer listening on 127.0.0.1:{server.Port}");
        await server.AcceptAndHandleConcurrently(cancellationToken);
    }
}
