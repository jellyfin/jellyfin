using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.MediaEncoding.Subtitles
{

    [TestClass]
    public class SrtParserTests
    {

        [TestMethod]
        public void TestParse()
        {

            var expectedSubs =
                new SubtitleTrackInfo
                {
                    TrackEvents = new List<SubtitleTrackEvent> {
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "1",
                                                                                                                    StartPositionTicks = 24000000,
                                                                                                                    EndPositionTicks = 52000000,
                                                                                                                    Text =
                                                                                                                        "[Background Music Playing]"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "2",
                                                                                                                    StartPositionTicks = 157120000,
                                                                                                                    EndPositionTicks = 173990000,
                                                                                                                    Text =
                                                                                                                        "Oh my god, Watch out!"+ParserValues.NewLine+"It's coming!!"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "3",
                                                                                                                    StartPositionTicks = 257120000,
                                                                                                                    EndPositionTicks = 303990000,
                                                                                                                    Text = "[Bird noises]"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "4",
                                                                                                                    StartPositionTicks = 310000000,
                                                                                                                    EndPositionTicks = 319990000,
                                                                                                                    Text =
                                                                                                                        "This text is <font color=\"red\">RED</font> and has not been positioned."
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "5",
                                                                                                                    StartPositionTicks = 320000000,
                                                                                                                    EndPositionTicks = 329990000,
                                                                                                                    Text =
                                                                                                                        "This is a"+ParserValues.NewLine+"new line, as is"+ParserValues.NewLine+"this"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "6",
                                                                                                                    StartPositionTicks = 330000000,
                                                                                                                    EndPositionTicks = 339990000,
                                                                                                                    Text =
                                                                                                                        "This contains nested <b>bold, <i>italic, <u>underline</u> and <s>strike-through</s></u></i></b> HTML tags"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "7",
                                                                                                                    StartPositionTicks = 340000000,
                                                                                                                    EndPositionTicks = 349990000,
                                                                                                                    Text =
                                                                                                                        "Unclosed but <b>supported HTML tags are left in,  SSA italics aren't"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "8",
                                                                                                                    StartPositionTicks = 350000000,
                                                                                                                    EndPositionTicks = 359990000,
                                                                                                                    Text =
                                                                                                                        "&lt;ggg&gt;Unsupported&lt;/ggg&gt; HTML tags are escaped and left in, even if &lt;hhh&gt;not closed."
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "9",
                                                                                                                    StartPositionTicks = 360000000,
                                                                                                                    EndPositionTicks = 369990000,
                                                                                                                    Text =
                                                                                                                        "Multiple SSA tags are stripped"
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "10",
                                                                                                                    StartPositionTicks = 370000000,
                                                                                                                    EndPositionTicks = 379990000,
                                                                                                                    Text =
                                                                                                                        "Greater than (&lt;) and less than (&gt;) are shown"
                                                                                                                }
                                                                                     }
                };

            var sut = new SrtParser(new NullLogger());

            var stream = File.OpenRead(@"MediaEncoding\Subtitles\TestSubtitles\unit.srt");

            var result = sut.Parse(stream, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSubs.TrackEvents.Count, result.TrackEvents.Count);
            for (int i = 0; i < expectedSubs.TrackEvents.Count; i++)
            {
                Assert.AreEqual(expectedSubs.TrackEvents[i].Id, result.TrackEvents[i].Id);
                Assert.AreEqual(expectedSubs.TrackEvents[i].StartPositionTicks, result.TrackEvents[i].StartPositionTicks);
                Assert.AreEqual(expectedSubs.TrackEvents[i].EndPositionTicks, result.TrackEvents[i].EndPositionTicks);
                Assert.AreEqual(expectedSubs.TrackEvents[i].Text, result.TrackEvents[i].Text);
            }

        }
    }
}