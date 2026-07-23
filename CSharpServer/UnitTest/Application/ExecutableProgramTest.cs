using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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

        [Fact]
        public async Task Server_ReturnsNetworkErrorAndExitCodeOne_WhenPortIsAlreadyInUse()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var result = await RunExecutableAsync("CSharpServer.dll", port.ToString());

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Server network error:", result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.DoesNotContain("System.Net.Sockets.SocketException", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        [Fact]
        public async Task Client_ReturnsNetworkErrorAndExitCodeOne_WhenRequestTimesOut()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var result = await RunExecutableAsync(
                "CSharpClient.dll",
                "127.0.0.1",
                port.ToString(),
                "hello",
                "100");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                "Client network error: Echo request did not complete before timeout.",
                result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.DoesNotContain("System.TimeoutException", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        [Fact]
        public async Task Client_ReturnsNetworkErrorAndExitCodeOne_WhenServerClosesBeforeResponse()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var serverTask = AcceptRequestAndCloseAsync(listener);

            var result = await RunExecutableAsync(
                "CSharpClient.dll",
                "127.0.0.1",
                port.ToString(),
                "hello",
                "2000");
            await serverTask;

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Client network error:", result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.DoesNotContain("System.", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        [Fact]
        public async Task Client_ReturnsProtocolErrorAndExitCodeOne_WhenResponseIsMalformed()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var serverTask = AcceptRequestAndSendMalformedResponseAsync(listener);

            var result = await RunExecutableAsync(
                "CSharpClient.dll",
                "127.0.0.1",
                port.ToString(),
                "hello",
                "2000");
            await serverTask;

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Client protocol error:", result.StandardError);
            Assert.DoesNotContain("Unhandled exception", result.StandardError);
            Assert.DoesNotContain("System.IO.InvalidDataException", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        private static async Task AcceptRequestAndCloseAsync(TcpListener listener)
        {
            using var client = await listener.AcceptTcpClientAsync();
            var request = new byte[sizeof(int) + "hello".Length];
            await client.GetStream().ReadExactlyAsync(request);
        }

        private static async Task AcceptRequestAndSendMalformedResponseAsync(TcpListener listener)
        {
            using var client = await listener.AcceptTcpClientAsync();
            var stream = client.GetStream();
            var request = new byte[sizeof(int) + "hello".Length];
            await stream.ReadExactlyAsync(request);

            var malformedHeader = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(
                malformedHeader,
                CSharpServer.Packet.ProtocolLimits.MaxPayloadLength + 1);
            await stream.WriteAsync(malformedHeader);
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
