using System.Text;
using CSharpServer.Packet;

namespace CSharpClient;

public sealed class ClientOptions
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;
    private const string DefaultMessage = "hello";
    private const int DefaultRequestTimeoutMilliseconds = 5000;

    private ClientOptions(string host, int port, string message, TimeSpan requestTimeout)
    {
        Host = host;
        Port = port;
        Message = message;
        RequestTimeout = requestTimeout;
    }

    public const string Usage =
        "Usage: CSharpClient [host] [port] [message] [request-timeout-ms]";

    public string Host { get; }
    public int Port { get; }
    public string Message { get; }
    public TimeSpan RequestTimeout { get; }

    public static bool TryParse(
        string[] args,
        out ClientOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        if (args.Length > 4)
        {
            error = $"Too many arguments.{Environment.NewLine}{Usage}";
            return false;
        }

        var host = args.Length > 0 ? args[0] : DefaultHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            error = $"Host cannot be empty.{Environment.NewLine}{Usage}";
            return false;
        }

        var port = DefaultPort;
        if (args.Length > 1
            && (!int.TryParse(args[1], out port) || port is < 1 or > 65535))
        {
            error = $"Port must be an integer from 1 to 65535.{Environment.NewLine}{Usage}";
            return false;
        }

        var message = args.Length > 2 ? args[2] : DefaultMessage;
        if (Encoding.UTF8.GetByteCount(message) > ProtocolLimits.MaxPayloadLength)
        {
            error = $"Message cannot exceed {ProtocolLimits.MaxPayloadLength} UTF-8 bytes."
                + $"{Environment.NewLine}{Usage}";
            return false;
        }

        var requestTimeoutMilliseconds = DefaultRequestTimeoutMilliseconds;
        if (args.Length > 3
            && (!int.TryParse(args[3], out requestTimeoutMilliseconds)
                || requestTimeoutMilliseconds <= 0))
        {
            error = $"Request timeout must be a positive integer.{Environment.NewLine}{Usage}";
            return false;
        }

        options = new ClientOptions(
            host,
            port,
            message,
            TimeSpan.FromMilliseconds(requestTimeoutMilliseconds));
        return true;
    }
}
