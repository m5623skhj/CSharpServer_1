using System.Diagnostics;
using CSharpClient;
using CSharpServer;

namespace UnitTest.Application
{
    public class ExecutableProgramTest
    {
        [Fact]
        public async Task Server_ReturnsUsageErrorAndExitCodeOne_WhenPortIsInvalid()
        {
            var result = await RunExecutableAsync("CSharpServer.dll", "invalid");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(ServerOptions.Usage, result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        [Fact]
        public async Task Client_ReturnsUsageErrorAndExitCodeOne_WhenPortIsInvalid()
        {
            var result = await RunExecutableAsync(
                "CSharpClient.dll",
                "127.0.0.1",
                "invalid");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(ClientOptions.Usage, result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        private static async Task<ProcessResult> RunExecutableAsync(
            string assemblyName,
            params string[] arguments)
        {
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, assemblyName);
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(assemblyPath);
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            Assert.True(process.Start());

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }

            return new ProcessResult(
                process.ExitCode,
                await standardOutputTask,
                await standardErrorTask);
        }

        private sealed record ProcessResult(
            int ExitCode,
            string StandardOutput,
            string StandardError);
    }
}
