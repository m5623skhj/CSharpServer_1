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

        var client = new EchoClient();

        var response = await client.SendEchoRequestAsync(
            options!.Host,
            options.Port,
            options.Message,
            options.RequestTimeout);

        Console.WriteLine(response);
        return 0;
    }
}
