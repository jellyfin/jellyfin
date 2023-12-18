using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class SsaParserTests
    {
        private readonly SubtitleEditParser _parser = new SubtitleEditParser(new NullLogger<SubtitleEditParser>());

        [Theory]
        [MemberData(nameof(Parse_MultipleDialogues_TestData))]
        public void Parse_MultipleDialogues_Success(string ssa, IReadOnlyList<SubtitleTrackEvent> expectedSubtitleTrackEvents)
        {
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(ssa));

            SubtitleTrackInfo subtitleTrackInfo = _parser.Parse(stream, "ssa");

            Assert.Equal(expectedSubtitleTrackEvents.Count, subtitleTrackInfo.TrackEvents.Count);

            for (int i = 0; i < expectedSubtitleTrackEvents.Count; ++i)
            {
                SubtitleTrackEvent expected = expectedSubtitleTrackEvents[i];
                SubtitleTrackEvent actual = subtitleTrackInfo.TrackEvents[i];

                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.Text, actual.Text);
                Assert.Equal(expected.StartPositionTicks, actual.StartPositionTicks);
                Assert.Equal(expected.EndPositionTicks, actual.EndPositionTicks);
            }
        }

        public static TheoryData<string, IReadOnlyList<SubtitleTrackEvent>> Parse_MultipleDialogues_TestData()
        {
            var data = new TheoryData<string, IReadOnlyList<SubtitleTrackEvent>>();

            data.Add(
                @"[Events]
                Format: Layer, Start, End, Text
                Dialogue: ,0:00:01.18,0:00:01.85,dialogue1
                Dialogue: ,0:00:02.18,0:00:02.85,dialogue2
                Dialogue: ,0:00:03.18,0:00:03.85,dialogue3
                ",
                new List<SubtitleTrackEvent>
                {
                    new SubtitleTrackEvent("1", "dialogue1")
                    {
                        StartPositionTicks = 11800000,
                        EndPositionTicks = 18500000
                    },
                    new SubtitleTrackEvent("2", "dialogue2")
                    {
                        StartPositionTicks = 21800000,
                        EndPositionTicks = 28500000
                    },
                    new SubtitleTrackEvent("3", "dialogue3")
                    {
                        StartPositionTicks = 31800000,
                        EndPositionTicks = 38500000
                    }
                });

            return data;
        }

        [Fact]
        public void Parse_Valid_Success()
        {
            using var stream = File.OpenRead("Test Data/example.ssa");

            var parsed = _parser.Parse(stream, "ssa");
            Assert.Single(parsed.TrackEvents);
            var trackEvent = parsed.TrackEvents[0];

            Assert.Equal("1", trackEvent.Id);
            Assert.Equal(TimeSpan.Parse("00:00:01.18", CultureInfo.InvariantCulture).Ticks, trackEvent.StartPositionTicks);
            Assert.Equal(TimeSpan.Parse("00:00:06.85", CultureInfo.InvariantCulture).Ticks, trackEvent.EndPositionTicks);
            Assert.Equal("{\\pos(400,570)}Like an angel with pity on nobody", trackEvent.Text);
        }
    }
}
