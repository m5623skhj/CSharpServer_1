using CSharpServer;

namespace UnitTest.Application
{
    public class ServerApplicationTest
    {
        [Fact]
        public async Task RunAsync_Returns_WhenCancellationIsRequested()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var application = new ServerApplication();
            Assert.True(ServerOptions.TryParse(["0"], out var options, out _));
            var runTask = application.RunAsync(options!, cancellationTokenSource.Token);

            await cancellationTokenSource.CancelAsync();

            await runTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
