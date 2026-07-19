using CSharpServer.Network;
using CSharpServer.Packet;

namespace CSharpServer.Content
{
    public static class EchoStreamConnectionFactory
    {
        public static StreamConnection Create(Stream stream, int inBufferSize)
        {
            var echoHandler = new EchoPacketHandler(payload => stream.Write(PacketEncoder.Encode(payload)));
            var connection = new StreamConnection(stream, inBufferSize, echoHandler.Handle);

            return connection;
        }
    }
}
