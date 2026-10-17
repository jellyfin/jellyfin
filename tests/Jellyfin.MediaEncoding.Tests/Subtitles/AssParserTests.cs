using System;
using System.Globalization;
using System.IO;
using MediaBrowser.MediaEncoding.Subtitles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class AssParserTests
    {
        [Fact]
        public void Parse_Valid_Success()
        {
            using var stream = File.OpenRead("Test Data/example.ass");

            var parsed = new SubtitleEditParser(new NullLogger<SubtitleEditParser>()).Parse(stream, "ass");
            Assert.Single(parsed.Paragraphs);
            var paragraph = parsed.Paragraphs[0];

            Assert.Equal(1, paragraph.Number);
            Assert.Equal(TimeSpan.Parse("00:00:01.18", CultureInfo.InvariantCulture).Ticks, paragraph.StartTime.TimeSpan.Ticks);
            Assert.Equal(TimeSpan.Parse("00:00:06.85", CultureInfo.InvariantCulture).Ticks, paragraph.EndTime.TimeSpan.Ticks);
            Assert.Equal("{\\pos(400,570)}Like an Angel with pity on nobody" + Environment.NewLine + "The second line in subtitle", paragraph.Text);
        }
    }
}
