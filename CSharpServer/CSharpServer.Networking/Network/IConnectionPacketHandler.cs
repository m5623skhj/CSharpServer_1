namespace CSharpServer.Network
{
    public interface IConnectionPacketHandler
    {
        void Handle(IConnectionSender sender, byte[] payload);

        ValueTask HandleAsync(
            IConnectionSender sender,
            byte[] payload,
            CancellationToken cancellationToken);
    }
}
