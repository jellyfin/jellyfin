using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeriesPathParserTest
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("The.Show.S01", "The.Show")]
        [InlineData("/The.Show.S01", "The.Show")]
        [InlineData("/some/place/The.Show.S01", "The.Show")]
        [InlineData("/something/The.Show.S01", "The.Show")]
        [InlineData("The Show Season 10", "The Show")]
        [InlineData("The Show S01E01", "The Show")]
        [InlineData("The Show S01E01 Episode", "The Show")]
        [InlineData("/something/The Show/Season 1", "The Show")]
        [InlineData("/something/The Show/S01", "The Show")]
        public void SeriesPathParserParseTest(string path, string name)
        {
            var res = SeriesPathParser.Parse(_namingOptions, path);

            Assert.Equal(name, res.SeriesName);
            Assert.True(res.Success);
        }
    }
}
