using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Model.Tests.Entities
{
    public class MediaStreamTests
    {
        [Theory]
        [InlineData("ASS")]
        [InlineData("SRT")]
        [InlineData("")]
        public void GetDisplayTitle_should_append_codec_for_subtitle(string codec)
        {
            var mediaStream = new MediaStream { Type = MediaStreamType.Subtitle, Title = "English", Codec = codec };

            Assert.Contains(codec, mediaStream.DisplayTitle, System.StringComparison.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("English", "", false, false, "ASS", "English - Und - ASS")]
        [InlineData("English", "", false, false, "", "English - Und")]
        [InlineData("English", "EN", false, false, "", "English")]
        [InlineData("English", "EN", true, true, "SRT", "English - Default - Forced - SRT")]
        [InlineData(null, null, false, false, null, "Und")]
        public void GetDisplayTitle_should_return_valid_for_subtitle(string title, string language, bool isForced, bool isDefault, string codec, string expected)
        {
            var mediaStream = new MediaStream { Type = MediaStreamType.Subtitle, Language = language, Title = title, IsForced = isForced, IsDefault = isDefault, Codec = codec };

            Assert.Equal(expected, mediaStream.DisplayTitle);
        }
    }
}
