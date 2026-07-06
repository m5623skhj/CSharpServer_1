using CSharpServer.Network;

namespace UnitTest.Network
{
    public class StreamConnectionTransportTest
    {
        [Fact]
        public void Send_WritesDataToStream()
        {
            using var stream = new MemoryStream();
            var transport = new StreamConnectionTransport(stream);
            var data = new byte[] { 0x01, 0x02, 0x03 };

            transport.Send(data);

            Assert.Equal(data, stream.ToArray());
        }

        [Fact]
        public void Close_ClosesStream()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            transport.Close();

            Assert.True(stream.IsDisposed);
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
