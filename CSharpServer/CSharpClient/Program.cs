namespace CSharpClient;

internal static class Program
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;
    private const string DefaultMessage = "hello";
    private const int DefaultResponseTimeoutMilliseconds = 5000;

    public static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : DefaultHost;
        var port = args.Length > 1 ? int.Parse(args[1]) : DefaultPort;
        var message = args.Length > 2 ? args[2] : DefaultMessage;
        var responseTimeoutMilliseconds = args.Length > 3
            ? int.Parse(args[3])
            : DefaultResponseTimeoutMilliseconds;
        var client = new EchoClient();

        var response = await client.SendEchoRequestAsync(
            host,
            port,
            message,
            TimeSpan.FromMilliseconds(responseTimeoutMilliseconds));

        Console.WriteLine(response);
    }
}
