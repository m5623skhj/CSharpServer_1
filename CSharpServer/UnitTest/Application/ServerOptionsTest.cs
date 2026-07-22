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

        [Fact]
        public void TryParse_ReturnsUsageError_WhenTooManyArgumentsAreProvided()
        {
            var result = ServerOptions.TryParse(["5000", "extra"], out var options, out var error);

            Assert.False(result);
            Assert.Null(options);
            Assert.Contains(ServerOptions.Usage, error);
        }
    }
}
