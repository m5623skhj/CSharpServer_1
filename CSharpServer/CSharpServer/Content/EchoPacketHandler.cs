using CSharpServer.Network;

namespace CSharpServer.Content
{
    public sealed class EchoPacketHandler : IConnectionPacketHandler
    {
        public void Handle(IConnectionSender sender, byte[] payload)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(payload);

            sender.Send(payload);
        }

        public ValueTask HandleAsync(
            IConnectionSender sender,
            byte[] payload,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(payload);

            return sender.SendAsync(payload, cancellationToken);
        }
    }
}
