namespace CSharpServer;

public sealed class ServerOptions
{
    private const int DefaultPort = 5000;
    private const int DefaultMaxConcurrentClients = 100;
    private const int DefaultClientIdleTimeoutMilliseconds = 30000;

    private ServerOptions(int port, int maxConcurrentClients, TimeSpan clientIdleTimeout)
    {
        Port = port;
        MaxConcurrentClients = maxConcurrentClients;
        ClientIdleTimeout = clientIdleTimeout;
    }

    public const string Usage =
        "Usage: CSharpServer [port] [max-concurrent-clients] [client-idle-timeout-ms]";

    public int Port { get; }
    public int MaxConcurrentClients { get; }
    public TimeSpan ClientIdleTimeout { get; }

    public static bool TryParse(
        string[] args,
        out ServerOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        if (args.Length > 3)
        {
            error = $"Too many arguments.{Environment.NewLine}{Usage}";
            return false;
        }

        var port = DefaultPort;
        if (args.Length >= 1
            && (!int.TryParse(args[0], out port) || port is < 0 or > 65535))
        {
            error = $"Port must be an integer from 0 to 65535.{Environment.NewLine}{Usage}";
            return false;
        }

        var maxConcurrentClients = DefaultMaxConcurrentClients;
        if (args.Length >= 2
            && (!int.TryParse(args[1], out maxConcurrentClients) || maxConcurrentClients <= 0))
        {
            error = $"Max concurrent clients must be a positive integer.{Environment.NewLine}{Usage}";
            return false;
        }

        var clientIdleTimeoutMilliseconds = DefaultClientIdleTimeoutMilliseconds;
        if (args.Length == 3
            && (!int.TryParse(args[2], out clientIdleTimeoutMilliseconds)
                || clientIdleTimeoutMilliseconds <= 0))
        {
            error = $"Client idle timeout must be a positive integer.{Environment.NewLine}{Usage}";
            return false;
        }

        options = new ServerOptions(
            port,
            maxConcurrentClients,
            TimeSpan.FromMilliseconds(clientIdleTimeoutMilliseconds));
        return true;
    }
}
