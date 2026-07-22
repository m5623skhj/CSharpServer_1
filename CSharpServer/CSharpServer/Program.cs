namespace CSharpServer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
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
            await application.RunAsync(args, cancellationTokenSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
