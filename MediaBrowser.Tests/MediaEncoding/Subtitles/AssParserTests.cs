using System.Text;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.MediaInfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Tests.MediaEncoding.Subtitles {

    [TestClass]
    public class AssParserTests {

        [TestMethod]
        public void TestParse() {

            var expectedSubs =
                new SubtitleTrackInfo {
                                          TrackEvents = new List<SubtitleTrackEvent> {
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "1",
                                                                                                                    StartPositionTicks = 24000000,
                                                                                                                    EndPositionTicks = 72000000,
                                                                                                                    Text =
                                                                                                                        "Senator, we're "+ParserValues.NewLine+"making our final "+ParserValues.NewLine+"approach into Coruscant."
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "2",
                                                                                                                    StartPositionTicks = 97100000,
                                                                                                                    EndPositionTicks = 133900000,
                                                                                                                    Text =
                                                                                                                        "Very good, Lieutenant."
                                                                                                                },
                                                                                         new SubtitleTrackEvent {
                                                                                                                    Id = "3",
                                                                                                                    StartPositionTicks = 150400000,
                                                                                                                    EndPositionTicks = 180400000,
                                                                                                                    Text = "It's "+ParserValues.NewLine+"a "+ParserValues.NewLine+"trap!"
                                                                                                                }
                                                                                     }
                                      };

            var sut = new AssParser();

            var stream = File.OpenRead(@"MediaEncoding\Subtitles\TestSubtitles\data.ass");

            var result = sut.Parse(stream, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSubs.TrackEvents.Count,result.TrackEvents.Count);
            for (int i = 0; i < expectedSubs.TrackEvents.Count; i++)
            {
                Assert.AreEqual(expectedSubs.TrackEvents[i].Id, result.TrackEvents[i].Id);
                Assert.AreEqual(expectedSubs.TrackEvents[i].StartPositionTicks, result.TrackEvents[i].StartPositionTicks);
                Assert.AreEqual(expectedSubs.TrackEvents[i].EndPositionTicks, result.TrackEvents[i].EndPositionTicks);
                Assert.AreEqual(expectedSubs.TrackEvents[i].Text, result.TrackEvents[i].Text);
            }

        }

        [TestMethod]
        public void TestParse2()
        {

            var sut = new AssParser();

            var stream = File.OpenRead(@"MediaEncoding\Subtitles\TestSubtitles\data2.ass");

            var result = sut.Parse(stream, CancellationToken.None);

            Assert.IsNotNull(result);

            using (var ms = new MemoryStream())
            {
                var writer = new SrtWriter();
                writer.Write(result, ms, CancellationToken.None);

                ms.Position = 0;
                var text = Encoding.UTF8.GetString(ms.ToArray());
                var b = text;
            }

        }
    }
}