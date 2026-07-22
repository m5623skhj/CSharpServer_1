using System.Net.Sockets;

namespace CSharpServer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!ServerOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;
        try
        {
            var application = new ServerApplication();
            await application.RunAsync(options!, cancellationTokenSource.Token);
        }
        catch (Exception exception) when (IsNetworkException(exception))
        {
            Console.Error.WriteLine($"Server network error: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }

    private static bool IsNetworkException(Exception exception)
    {
        return exception is IOException or SocketException;
    }
}
