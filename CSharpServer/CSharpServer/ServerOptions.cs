namespace CSharpServer;

public sealed class ServerOptions
{
    private const int DefaultPort = 5000;

    private ServerOptions(int port)
    {
        Port = port;
    }

    public const string Usage = "Usage: CSharpServer [port]";

    public int Port { get; }

    public static bool TryParse(
        string[] args,
        out ServerOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        if (args.Length > 1)
        {
            error = $"Too many arguments.{Environment.NewLine}{Usage}";
            return false;
        }

        var port = DefaultPort;
        if (args.Length == 1
            && (!int.TryParse(args[0], out port) || port is < 0 or > 65535))
        {
            error = $"Port must be an integer from 0 to 65535.{Environment.NewLine}{Usage}";
            return false;
        }

        options = new ServerOptions(port);
        return true;
    }
}
