using CSharpServer.Network;

namespace CSharpServer.Content
{
    public static class EchoStreamConnectionFactory
    {
        public static StreamConnection Create(Stream stream, int inBufferSize)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            return new StreamConnection(
                stream,
                inBufferSize,
                new EchoPacketHandler());
        }
    }
}
