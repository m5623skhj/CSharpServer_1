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
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }
}
