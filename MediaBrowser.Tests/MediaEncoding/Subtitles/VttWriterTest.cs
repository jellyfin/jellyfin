using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.MediaInfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.MediaEncoding.Subtitles {

    [TestClass]
    public class VttWriterTest {
        [TestMethod]
        public void TestWrite() {
            var infoSubs =
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
                                                                                                                        "Oh my god, Watch out!<br />It's coming!!"
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
                                                                                                                        "This is a<br />new line, as is<br />this"
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

            var sut = new VttWriter();

            if(File.Exists("testVTT.vtt"))
                File.Delete("testVTT.vtt");
            using (var file = File.OpenWrite("testVTT.vtt"))
            {
                sut.Write(infoSubs, file, CancellationToken.None);
            }

            var result = File.ReadAllText("testVTT.vtt");
            var expectedText = File.ReadAllText(@"MediaEncoding\Subtitles\TestSubtitles\expected.vtt");
            
            Assert.AreEqual(expectedText, result);
        }
    }
}