using System;
using Emby.Naming.Common;
using Emby.Naming.Subtitles;
using Xunit;

namespace Jellyfin.Naming.Tests.Subtitles
{
    public class SubtitleParserTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("The Skin I Live In (2011).srt", null, false, false)]
        [InlineData("The Skin I Live In (2011).eng.srt", "eng", false, false)]
        [InlineData("The Skin I Live In (2011).eng.default.srt", "eng", true, false)]
        [InlineData("The Skin I Live In (2011).eng.forced.srt", "eng", false, true)]
        [InlineData("The Skin I Live In (2011).eng.foreign.srt", "eng", false, true)]
        [InlineData("The Skin I Live In (2011).eng.default.foreign.srt", "eng", true, true)]
        [InlineData("The Skin I Live In (2011).default.foreign.eng.srt", "eng", true, true)]
        public void SubtitleParser_ValidFileName_Parses(string input, string language, bool isDefault, bool isForced)
        {
            var parser = new SubtitleParser(_namingOptions);

            var result = parser.ParseFile(input);

            Assert.Equal(language, result?.Language, true);
            Assert.Equal(isDefault, result?.IsDefault);
            Assert.Equal(isForced, result?.IsForced);
        }

        [Theory]
        [InlineData("The Skin I Live In (2011).mp4")]
        public void SubtitleParser_InvalidFileName_ReturnsNull(string input)
        {
            var parser = new SubtitleParser(_namingOptions);

            Assert.Null(parser.ParseFile(input));
        }

        [Fact]
        public void SubtitleParser_EmptyFileName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SubtitleParser(_namingOptions).ParseFile(string.Empty));
        }
    }
}
