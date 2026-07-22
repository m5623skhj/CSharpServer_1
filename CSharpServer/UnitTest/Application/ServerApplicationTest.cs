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
            var runTask = application.RunAsync(["0"], cancellationTokenSource.Token);

            await cancellationTokenSource.CancelAsync();

            await runTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
