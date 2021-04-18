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
    }
}
