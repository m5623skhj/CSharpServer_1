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

        [Fact]
        public void Close_ClosesStreamOnlyOnce_WhenCalledMultipleTimes()
        {
            using var stream = new TrackingStream();
            var transport = new StreamConnectionTransport(stream);

            transport.Close();
            transport.Close();

            Assert.Equal(1, stream.DisposeCount);
        }

        [Fact]
        public async Task Close_WaitsForActiveSendToComplete()
        {
            using var stream = new BlockingWriteStream();
            var transport = new StreamConnectionTransport(stream);
            var sendTask = Task.Run(() => transport.Send([0x01]));

            Assert.True(stream.WriteStarted.Wait(TimeSpan.FromSeconds(1)));

            using var closeStarted = new ManualResetEventSlim();
            var closeTask = Task.Run(() =>
            {
                closeStarted.Set();
                transport.Close();
            });

            Assert.True(closeStarted.Wait(TimeSpan.FromSeconds(1)));
            var closedBeforeSendCompleted = stream.CloseCalled.Wait(TimeSpan.FromMilliseconds(100));

            stream.AllowWriteToComplete.Set();
            await Task.WhenAll(sendTask, closeTask);

            Assert.False(closedBeforeSendCompleted);
            Assert.True(stream.CloseCalled.IsSet);
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool IsDisposed { get; private set; }
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        private sealed class BlockingWriteStream : MemoryStream
        {
            public ManualResetEventSlim WriteStarted { get; } = new();
            public ManualResetEventSlim AllowWriteToComplete { get; } = new();
            public ManualResetEventSlim CloseCalled { get; } = new();

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteStarted.Set();
                AllowWriteToComplete.Wait();
                base.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                CloseCalled.Set();
                base.Dispose(disposing);
            }
        }
    }
}
