using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class SsaParserTests
    {
        // commonly shared invariant value between tests, assumes default format order
        private const string InvariantDialoguePrefix = "[Events]\nDialogue: ,0:00:00.00,0:00:00.01,,,,,,,";

        private SsaParser parser = new SsaParser();

        [Theory]
        [InlineData("[EvEnTs]\nDialogue: ,0:00:00.00,0:00:00.01,,,,,,,text", "text")] // label casing insensitivity
        [InlineData("[Events]\n,0:00:00.00,0:00:00.01,,,,,,,labelless dialogue", "labelless dialogue")] // no "Dialogue:" label, it is optional
        [InlineData("[Events]\nFormat: Text, Start, End, Layer, Effect, Style\nDialogue: reordered text,0:00:00.00,0:00:00.01", "reordered text")] // reordered formats
        [InlineData(InvariantDialoguePrefix + "Cased TEXT", "Cased TEXT")] // preserve text casing
        [InlineData(InvariantDialoguePrefix + "  text  ", "  text  ")] // do not trim text
        [InlineData(InvariantDialoguePrefix + "text, more text", "text, more text")] // append excess dialogue values (> 10) to text
        [InlineData(InvariantDialoguePrefix + "start {\\fnFont Name}text{\\fn} end", "start <font face=\"Font Name\">text</font> end")] // font name
        [InlineData(InvariantDialoguePrefix + "start {\\fs10}text{\\fs} end", "start <font size=\"10\">text</font> end")] // font size
        [InlineData(InvariantDialoguePrefix + "start {\\c&H112233}text{\\c} end", "start <font color=\"#332211\">text</font> end")] // color
        [InlineData(InvariantDialoguePrefix + "start {\\1c&H112233}text{\\1c} end", "start <font color=\"#332211\">text</font> end")] // primay color
        [InlineData(InvariantDialoguePrefix + "start {\\fnFont Name}text1 {\\fs10}text2{\\fs}{\\fn} {\\1c&H112233}text3{\\1c} end", "start <font face=\"Font Name\">text1 <font size=\"10\">text2</font></font> <font color=\"#332211\">text3</font> end")] // nested formatting
        public void Parse(string ssa, string expectedText)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(ssa)))
            {
                SubtitleTrackInfo subtitleTrackInfo = parser.Parse(stream, CancellationToken.None);
                SubtitleTrackEvent actual = subtitleTrackInfo.TrackEvents[0];
                Assert.Equal(expectedText, actual.Text);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_MultipleDialogues_TestData))]
        public void Parse_MultipleDialogues(string ssa, IReadOnlyList<SubtitleTrackEvent> expectedSubtitleTrackEvents)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(ssa)))
            {
                SubtitleTrackInfo subtitleTrackInfo = parser.Parse(stream, CancellationToken.None);

                Assert.Equal(expectedSubtitleTrackEvents.Count, subtitleTrackInfo.TrackEvents.Count);

                for (int i = 0; i < expectedSubtitleTrackEvents.Count; ++i)
                {
                    SubtitleTrackEvent expected = expectedSubtitleTrackEvents[i];
                    SubtitleTrackEvent actual = subtitleTrackInfo.TrackEvents[i];

                    Assert.Equal(expected.StartPositionTicks, actual.StartPositionTicks);
                    Assert.Equal(expected.EndPositionTicks, actual.EndPositionTicks);
                    Assert.Equal(expected.Text, actual.Text);
                }
            }
        }

        public static IEnumerable<object[]> Parse_MultipleDialogues_TestData()
        {
            yield return new object[]
            {
                @"[Events]
                Format: Layer, Start, End, Text
                Dialogue: ,0:00:01.18,0:00:01.85,dialogue1
                Dialogue: ,0:00:02.18,0:00:02.85,dialogue2
                Dialogue: ,0:00:03.18,0:00:03.85,dialogue3
                ",
                new List<SubtitleTrackEvent>
                {
                    new SubtitleTrackEvent
                    {
                        StartPositionTicks = 11800000,
                        EndPositionTicks = 18500000,
                        Text = "dialogue1"
                    },
                    new SubtitleTrackEvent
                    {
                        StartPositionTicks = 21800000,
                        EndPositionTicks = 28500000,
                        Text = "dialogue2"
                    },
                    new SubtitleTrackEvent
                    {
                        StartPositionTicks = 31800000,
                        EndPositionTicks = 38500000,
                        Text = "dialogue3"
                    }
                }
            };
        }
    }
}
