using CSharpServer.Network;

namespace CSharpServer.Content
{
    public static class EchoStreamConnectionFactory
    {
        public static StreamConnection Create(Stream stream, int inBufferSize)
        {
            StreamConnection? connection = null;
            var echoHandler = new EchoPacketHandler(payload => connection!.Send(payload));
            connection = new StreamConnection(stream, inBufferSize, echoHandler.Handle);

            return connection;
        }
    }
}
