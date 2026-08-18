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

        [Fact]
        public async Task RunAsync_DoesNotCaptureSynchronizationContext()
        {
            using var cancellation = new CancellationTokenSource();
            Assert.True(ServerOptions.TryParse(["0"], out var options, out _));
            var context = new QueueingSynchronizationContext();
            var applicationStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var completion = new TaskCompletionSource<Exception?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(context);
                try
                {
                    var application = new ServerApplication();
                    var runTask = application.RunAsync(options!, cancellation.Token);
                    applicationStarted.TrySetResult();
                    runTask.GetAwaiter().GetResult();
                    completion.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    completion.TrySetResult(exception);
                }
            })
            {
                IsBackground = true
            };

            thread.Start();
            try
            {
                await applicationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
                await cancellation.CancelAsync();
                var applicationContinuation = await Task.WhenAny(
                        completion.Task,
                        context.ContinuationPosted.Task)
                    .WaitAsync(TimeSpan.FromSeconds(1));

                Assert.Same(completion.Task, applicationContinuation);
                Assert.Null(await completion.Task);
            }
            finally
            {
                await cancellation.CancelAsync();
                context.RunQueuedCallbacks();
                Assert.True(thread.Join(TimeSpan.FromSeconds(1)));
            }
        }

        private sealed class QueueingSynchronizationContext : SynchronizationContext
        {
            private readonly Queue<(SendOrPostCallback Callback, object? State)> callbacks = [];
            private readonly object callbacksLock = new();

            public TaskCompletionSource ContinuationPosted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public override void Post(SendOrPostCallback callback, object? state)
            {
                lock (callbacksLock)
                {
                    callbacks.Enqueue((callback, state));
                }

                ContinuationPosted.TrySetResult();
            }

            public void RunQueuedCallbacks()
            {
                while (true)
                {
                    (SendOrPostCallback Callback, object? State) callback;
                    lock (callbacksLock)
                    {
                        if (!callbacks.TryDequeue(out callback))
                        {
                            return;
                        }
                    }

                    callback.Callback(callback.State);
                }
            }
        }
    }
}
