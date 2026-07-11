namespace CSharpClient;

internal static class Program
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;
    private const string DefaultMessage = "hello";

    public static void Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : DefaultHost;
        var port = args.Length > 1 ? int.Parse(args[1]) : DefaultPort;
        var message = args.Length > 2 ? args[2] : DefaultMessage;
        var client = new EchoClient();

        var response = client.SendEchoRequest(host, port, message);

        Console.WriteLine(response);
    }
}
