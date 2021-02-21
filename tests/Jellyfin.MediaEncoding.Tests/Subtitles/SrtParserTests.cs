using System;
using System.Globalization;
using System.IO;
using System.Threading;
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
            using (var stream = File.OpenRead("Test Data/example.srt"))
            {
                var parsed = new SrtParser(new NullLogger<SrtParser>()).Parse(stream, CancellationToken.None);
                Assert.Equal(2, parsed.TrackEvents.Count);

                var trackEvent1 = parsed.TrackEvents[0];
                Assert.Equal("1", trackEvent1.Id);
                Assert.Equal(TimeSpan.Parse("00:02:17.440", CultureInfo.InvariantCulture).Ticks, trackEvent1.StartPositionTicks);
                Assert.Equal(TimeSpan.Parse("00:02:20.375", CultureInfo.InvariantCulture).Ticks, trackEvent1.EndPositionTicks);
                Assert.Equal("Senator, we're making\r\nour final approach into Coruscant.", trackEvent1.Text);

                var trackEvent2 = parsed.TrackEvents[1];
                Assert.Equal("2", trackEvent2.Id);
                Assert.Equal(TimeSpan.Parse("00:02:20.476", CultureInfo.InvariantCulture).Ticks, trackEvent2.StartPositionTicks);
                Assert.Equal(TimeSpan.Parse("00:02:22.501", CultureInfo.InvariantCulture).Ticks, trackEvent2.EndPositionTicks);
                Assert.Equal("Very good, Lieutenant.", trackEvent2.Text);
            }
        }
    }
}
