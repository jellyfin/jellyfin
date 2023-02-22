using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.Common
{
    public class NamingOptionsTest
    {
        [Fact]
        public void TestNamingOptionsCompile()
        {
            var options = new NamingOptions();

            Assert.NotEmpty(options.CleanDateTimeRegexes);
            Assert.NotEmpty(options.CleanStringRegexes);
        }

        [Fact]
        public void TestNamingOptionsEpisodeExpressions()
        {
            var exp = new EpisodeExpression(string.Empty);

            Assert.False(exp.IsOptimistic);
            exp.IsOptimistic = true;
            Assert.True(exp.IsOptimistic);

            Assert.Equal(string.Empty, exp.Expression);
            Assert.NotNull(exp.Regex);
            exp.Expression = "test";
            Assert.Equal("test", exp.Expression);
            Assert.NotNull(exp.Regex);
        }
    }
}
