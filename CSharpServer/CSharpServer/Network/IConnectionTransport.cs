namespace CSharpServer.Network
{
    public interface IConnectionTransport
    {
        void Send(byte[] data);
        ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
        void Close();
    }
}
