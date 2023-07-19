using System;
using System.Globalization;
using System.IO;
using MediaBrowser.MediaEncoding.Subtitles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class SrtParserTests
    {
        [Fact]
        public void Parse_Valid_Success()
        {
            using var stream = File.OpenRead("Test Data/example.srt");

            var parsed = new SubtitleEditParser(new NullLogger<SubtitleEditParser>()).Parse(stream, "srt");
            Assert.Equal(2, parsed.TrackEvents.Count);

            var trackEvent1 = parsed.TrackEvents[0];
            Assert.Equal("1", trackEvent1.Id);
            Assert.Equal(TimeSpan.Parse("00:02:17.440", CultureInfo.InvariantCulture).Ticks, trackEvent1.StartPositionTicks);
            Assert.Equal(TimeSpan.Parse("00:02:20.375", CultureInfo.InvariantCulture).Ticks, trackEvent1.EndPositionTicks);
            Assert.Equal("Senator, we're making" + Environment.NewLine + "our final approach into Coruscant.", trackEvent1.Text);

            var trackEvent2 = parsed.TrackEvents[1];
            Assert.Equal("2", trackEvent2.Id);
            Assert.Equal(TimeSpan.Parse("00:02:20.476", CultureInfo.InvariantCulture).Ticks, trackEvent2.StartPositionTicks);
            Assert.Equal(TimeSpan.Parse("00:02:22.501", CultureInfo.InvariantCulture).Ticks, trackEvent2.EndPositionTicks);
            Assert.Equal("Very good, Lieutenant.", trackEvent2.Text);
        }

        [Fact]
        public void Parse_EmptyNewlineBetweenText_Success()
        {
            using var stream = File.OpenRead("Test Data/example2.srt");

            var parsed = new SubtitleEditParser(new NullLogger<SubtitleEditParser>()).Parse(stream, "srt");
            Assert.Equal(2, parsed.TrackEvents.Count);

            var trackEvent1 = parsed.TrackEvents[0];
            Assert.Equal("311", trackEvent1.Id);
            Assert.Equal(TimeSpan.Parse("00:16:46.465", CultureInfo.InvariantCulture).Ticks, trackEvent1.StartPositionTicks);
            Assert.Equal(TimeSpan.Parse("00:16:49.009", CultureInfo.InvariantCulture).Ticks, trackEvent1.EndPositionTicks);
            Assert.Equal("Una vez que la gente se entere" + Environment.NewLine + Environment.NewLine + "de que ustedes están aquí,", trackEvent1.Text);

            var trackEvent2 = parsed.TrackEvents[1];
            Assert.Equal("312", trackEvent2.Id);
            Assert.Equal(TimeSpan.Parse("00:16:49.092", CultureInfo.InvariantCulture).Ticks, trackEvent2.StartPositionTicks);
            Assert.Equal(TimeSpan.Parse("00:16:51.470", CultureInfo.InvariantCulture).Ticks, trackEvent2.EndPositionTicks);
            Assert.Equal("este lugar se convertirá" + Environment.NewLine + Environment.NewLine + "en un maldito zoológico.", trackEvent2.Text);
        }
    }
}
