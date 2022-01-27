using Emby.Naming.Common;
using Emby.Naming.Subtitles;
using Xunit;

namespace Jellyfin.Naming.Tests.Subtitles
{
    public class SubtitleFilePathParserTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("The Skin I Live In (2011).srt", false, false)]
        [InlineData("The Skin I Live In (2011).eng.srt", false, false)]
        [InlineData("The Skin I Live In (2011).default.srt", true, false)]
        [InlineData("The Skin I Live In (2011).forced.srt", false, true)]
        [InlineData("The Skin I Live In (2011).eng.foreign.srt", false, true)]
        [InlineData("The Skin I Live In (2011).eng.default.foreign.srt", true, true)]
        [InlineData("The Skin I Live In (2011).default.foreign.eng.srt", true, true)]
        public void SubtitleFilePathParser_ValidFileName_Parses(string input, bool isDefault, bool isForced)
        {
            var parser = new SubtitleFilePathParser(_namingOptions);

            var result = parser.ParseFile(input);

            Assert.Equal(isDefault, result?.IsDefault);
            Assert.Equal(isForced, result?.IsForced);
            Assert.Equal(input, result?.Path);
        }

        [Theory]
        [InlineData("The Skin I Live In (2011).mp4")]
        [InlineData("")]
        public void SubtitleFilePathParser_InvalidFileName_ReturnsNull(string input)
        {
            var parser = new SubtitleFilePathParser(_namingOptions);

            Assert.Null(parser.ParseFile(input));
        }
    }
}
