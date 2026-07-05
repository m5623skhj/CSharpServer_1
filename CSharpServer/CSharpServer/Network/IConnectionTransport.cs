namespace CSharpServer.Network
{
    public interface IConnectionTransport
    {
        void Send(byte[] data);
        void Close();
    }
}
