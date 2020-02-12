using Emby.Naming.Common;
using Emby.Naming.Subtitles;
using Xunit;

namespace Jellyfin.Naming.Tests.Subtitles
{
    public class SubtitleParserTests
    {
        private SubtitleParser GetParser()
        {
            var options = new NamingOptions();

            return new SubtitleParser(options);
        }

        [Fact]
        public void TestSubtitles()
        {
            Test("The Skin I Live In (2011).srt", null, false, false);
            Test("The Skin I Live In (2011).eng.srt", "eng", false, false);
            Test("The Skin I Live In (2011).eng.default.srt", "eng", true, false);
            Test("The Skin I Live In (2011).eng.forced.srt", "eng", false, true);
            Test("The Skin I Live In (2011).eng.foreign.srt", "eng", false, true);
            Test("The Skin I Live In (2011).eng.default.foreign.srt", "eng", true, true);
            Test("The Skin I Live In (2011).default.foreign.eng.srt", "eng", true, true);
        }

        private void Test(string input, string language, bool isDefault, bool isForced)
        {
            var parser = GetParser();

            var result = parser.ParseFile(input);

            Assert.Equal(language, result.Language, true);
            Assert.Equal(isDefault, result.IsDefault);
            Assert.Equal(isForced, result.IsForced);
        }
    }
}
