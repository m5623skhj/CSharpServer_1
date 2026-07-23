using CSharpServer.Network;
using CSharpServer.Packet;

namespace CSharpServer.Content
{
    public static class EchoStreamConnectionFactory
    {
        public static StreamConnection Create(Stream stream, int inBufferSize)
        {
            var transport = new StreamConnectionTransport(stream);
            var echoHandler = new EchoPacketHandler(
                payload => transport.Send(PacketEncoder.Encode(payload)),
                (payload, cancellationToken) => transport.SendAsync(
                    PacketEncoder.Encode(payload),
                    cancellationToken));
            var connection = new StreamConnection(
                stream,
                inBufferSize,
                echoHandler.Handle,
                echoHandler.HandleAsync,
                transport);

            return connection;
        }
    }
}
