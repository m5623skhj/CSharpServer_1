using System.Net.Sockets;

namespace CSharpClient;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!ClientOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        try
        {
            var client = new EchoClient();

            var response = await client.SendEchoRequestAsync(
                options!.Host,
                options.Port,
                options.Message,
                options.RequestTimeout);

            Console.WriteLine(response);
            return 0;
        }
        catch (Exception exception) when (IsNetworkException(exception))
        {
            Console.Error.WriteLine($"Client network error: {exception.Message}");
            return 1;
        }
    }

    private static bool IsNetworkException(Exception exception)
    {
        return exception is IOException
            or SocketException
            or TimeoutException;
    }
}
