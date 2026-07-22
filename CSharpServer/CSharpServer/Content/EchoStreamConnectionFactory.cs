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
                payload => transport.Send(PacketEncoder.Encode(payload)));
            var connection = new StreamConnection(
                stream,
                inBufferSize,
                echoHandler.Handle,
                transport);

            return connection;
        }
    }
}
