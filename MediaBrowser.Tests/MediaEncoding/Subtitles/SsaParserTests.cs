using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.MediaEncoding.Subtitles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.MediaEncoding.Subtitles {

    [TestClass]
    public class SsaParserTests {

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
                                                                                                                        "Senator, we're <br />making our final <br />approach into Coruscant."
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
                                                                                                                    Text = "It's <br />a <br />trap!"
                                                                                                                }
                                                                                     }
                                      };

            var sut = new SsaParser();

            var stream = File.OpenRead(@"MediaEncoding\Subtitles\TestSubtitles\data.ssa");

            var result = sut.Parse(stream);

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
    }
}