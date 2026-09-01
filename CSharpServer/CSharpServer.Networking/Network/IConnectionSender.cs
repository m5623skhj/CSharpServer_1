namespace CSharpServer.Network
{
    public interface IConnectionSender
    {
        void Send(byte[] payload);
        ValueTask SendAsync(byte[] payload, CancellationToken cancellationToken);
    }
}
