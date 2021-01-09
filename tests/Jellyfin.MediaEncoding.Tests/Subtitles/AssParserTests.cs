using System;
using System.Globalization;
using System.IO;
using System.Threading;
using MediaBrowser.MediaEncoding.Subtitles;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class AssParserTests
    {
        [Fact]
        public void Parse_Valid_Success()
        {
            using (var stream = File.OpenRead("Test Data/example.ass"))
            {
                var parsed = new AssParser().Parse(stream, CancellationToken.None);
                Assert.Single(parsed.TrackEvents);
                var trackEvent = parsed.TrackEvents[0];

                Assert.Equal("1", trackEvent.Id);
                Assert.Equal(TimeSpan.Parse("00:00:01.18", CultureInfo.InvariantCulture).Ticks, trackEvent.StartPositionTicks);
                Assert.Equal(TimeSpan.Parse("00:00:06.85", CultureInfo.InvariantCulture).Ticks, trackEvent.EndPositionTicks);
                Assert.Equal("Like an Angel with pity on nobody\r\nThe second line in subtitle", trackEvent.Text);
            }
        }

        [Fact]
        public void ParseFieldHeaders_Valid_Success()
        {
            const string Line = "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text";
            var headers = AssParser.ParseFieldHeaders(Line);
            Assert.Equal(1, headers["Start"]);
            Assert.Equal(2, headers["End"]);
            Assert.Equal(9, headers["Text"]);
        }
    }
}
