using CSharpClient;
using CSharpServer.Packet;

namespace UnitTest.Client
{
    public class ClientOptionsTest
    {
        [Fact]
        public void TryParse_UsesDefaults_WhenArgumentsAreEmpty()
        {
            var result = ClientOptions.TryParse([], out var options, out var error);

            Assert.True(result);
            Assert.NotNull(options);
            Assert.Equal("127.0.0.1", options.Host);
            Assert.Equal(5000, options.Port);
            Assert.Equal("hello", options.Message);
            Assert.Equal(TimeSpan.FromSeconds(5), options.ResponseTimeout);
            Assert.Null(error);
        }

        [Fact]
        public void TryParse_UsesAllProvidedArguments()
        {
            var result = ClientOptions.TryParse(
                ["localhost", "6000", "world", "2500"],
                out var options,
                out var error);

            Assert.True(result);
            Assert.NotNull(options);
            Assert.Equal("localhost", options.Host);
            Assert.Equal(6000, options.Port);
            Assert.Equal("world", options.Message);
            Assert.Equal(TimeSpan.FromMilliseconds(2500), options.ResponseTimeout);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("65536")]
        public void TryParse_ReturnsUsageError_WhenPortIsInvalid(string port)
        {
            var result = ClientOptions.TryParse(
                ["localhost", port],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ClientOptions.Usage, error);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public void TryParse_ReturnsUsageError_WhenTimeoutIsInvalid(string timeoutMilliseconds)
        {
            var result = ClientOptions.TryParse(
                ["localhost", "5000", "hello", timeoutMilliseconds],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ClientOptions.Usage, error);
        }

        [Fact]
        public void TryParse_ReturnsUsageError_WhenTooManyArgumentsAreProvided()
        {
            var result = ClientOptions.TryParse(
                ["localhost", "5000", "hello", "5000", "extra"],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ClientOptions.Usage, error);
        }

        [Fact]
        public void TryParse_ReturnsUsageError_WhenMessageExceedsProtocolLimit()
        {
            var message = new string(
                '\u20AC',
                (ProtocolLimits.MaxPayloadLength / 3) + 1);

            var result = ClientOptions.TryParse(
                ["localhost", "5000", message],
                out var options,
                out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ProtocolLimits.MaxPayloadLength.ToString(), error);
            Assert.Contains(ClientOptions.Usage, error);
        }
    }
}
