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
            Assert.Equal(2, parsed.Paragraphs.Count);

            var paragraph1 = parsed.Paragraphs[0];
            Assert.Equal(1, paragraph1.Number);
            Assert.Equal(TimeSpan.Parse("00:02:17.440", CultureInfo.InvariantCulture).Ticks, paragraph1.StartTime.TimeSpan.Ticks);
            Assert.Equal(TimeSpan.Parse("00:02:20.375", CultureInfo.InvariantCulture).Ticks, paragraph1.EndTime.TimeSpan.Ticks);
            Assert.Equal("Senator, we're making" + Environment.NewLine + "our final approach into Coruscant.", paragraph1.Text);

            var paragraph2 = parsed.Paragraphs[1];
            Assert.Equal(2, paragraph2.Number);
            Assert.Equal(TimeSpan.Parse("00:02:20.476", CultureInfo.InvariantCulture).Ticks, paragraph2.StartTime.TimeSpan.Ticks);
            Assert.Equal(TimeSpan.Parse("00:02:22.501", CultureInfo.InvariantCulture).Ticks, paragraph2.EndTime.TimeSpan.Ticks);
            Assert.Equal("Very good, Lieutenant.", paragraph2.Text);
        }

        [Fact]
        public void Parse_EmptyNewlineBetweenText_Success()
        {
            using var stream = File.OpenRead("Test Data/example2.srt");

            var parsed = new SubtitleEditParser(new NullLogger<SubtitleEditParser>()).Parse(stream, "srt");
            Assert.Equal(2, parsed.Paragraphs.Count);

            var paragraph1 = parsed.Paragraphs[0];
            Assert.Equal(311, paragraph1.Number);
            Assert.Equal(TimeSpan.Parse("00:16:46.465", CultureInfo.InvariantCulture).Ticks, paragraph1.StartTime.TimeSpan.Ticks);
            Assert.Equal(TimeSpan.Parse("00:16:49.009", CultureInfo.InvariantCulture).Ticks, paragraph1.EndTime.TimeSpan.Ticks);
            Assert.Equal("Una vez que la gente se entere" + Environment.NewLine + Environment.NewLine + "de que ustedes están aquí,", paragraph1.Text);

            var paragraph2 = parsed.Paragraphs[1];
            Assert.Equal(312, paragraph2.Number);
            Assert.Equal(TimeSpan.Parse("00:16:49.092", CultureInfo.InvariantCulture).Ticks, paragraph2.StartTime.TimeSpan.Ticks);
            Assert.Equal(TimeSpan.Parse("00:16:51.470", CultureInfo.InvariantCulture).Ticks, paragraph2.EndTime.TimeSpan.Ticks);
            Assert.Equal("este lugar se convertirá" + Environment.NewLine + Environment.NewLine + "en un maldito zoológico.", paragraph2.Text);
        }
    }
}
