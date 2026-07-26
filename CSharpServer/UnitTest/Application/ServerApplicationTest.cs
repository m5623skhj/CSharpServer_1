using CSharpServer;

namespace UnitTest.Application
{
    public class ServerApplicationTest
    {
        [Fact]
        public async Task RunAsync_ThrowsArgumentNullException_WhenOptionsIsNull()
        {
            var application = new ServerApplication();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                application.RunAsync(null!, CancellationToken.None));
        }

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
