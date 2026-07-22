using CSharpServer;

namespace UnitTest.Application
{
    public class ServerOptionsTest
    {
        [Fact]
        public void TryParse_UsesDefaultPort_WhenArgumentsAreEmpty()
        {
            var result = ServerOptions.TryParse([], out var options, out var error);

            Assert.True(result);
            Assert.NotNull(options);
            Assert.Equal(5000, options.Port);
            Assert.Equal(100, options.MaxConcurrentClients);
            Assert.Equal(TimeSpan.FromSeconds(30), options.ClientIdleTimeout);
            Assert.Null(error);
        }

        [Fact]
        public void TryParse_UsesProvidedConnectionLimits_WhenArgumentsAreValid()
        {
            var result = ServerOptions.TryParse(["5001", "25", "15000"], out var options, out var error);

            Assert.True(result);
            Assert.NotNull(options);
            Assert.Equal(5001, options.Port);
            Assert.Equal(25, options.MaxConcurrentClients);
            Assert.Equal(TimeSpan.FromSeconds(15), options.ClientIdleTimeout);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("-1")]
        [InlineData("65536")]
        public void TryParse_ReturnsUsageError_WhenPortIsInvalid(string port)
        {
            var result = ServerOptions.TryParse([port], out var options, out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ServerOptions.Usage, error);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public void TryParse_ReturnsUsageError_WhenMaxConcurrentClientsIsInvalid(string maxConcurrentClients)
        {
            var result = ServerOptions.TryParse(
                ["5000", maxConcurrentClients],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ServerOptions.Usage, error);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public void TryParse_ReturnsUsageError_WhenClientIdleTimeoutIsInvalid(string clientIdleTimeout)
        {
            var result = ServerOptions.TryParse(
                ["5000", "100", clientIdleTimeout],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ServerOptions.Usage, error);
        }

        [Fact]
        public void TryParse_ReturnsUsageError_WhenTooManyArgumentsAreProvided()
        {
            var result = ServerOptions.TryParse(
                ["5000", "100", "30000", "extra"],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ServerOptions.Usage, error);
        }
    }
}
