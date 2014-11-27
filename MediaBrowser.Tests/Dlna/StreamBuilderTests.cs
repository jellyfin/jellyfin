using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dlna.Profiles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Tests.Dlna
{
    [TestClass]
    public class StreamBuilderTests
    {
        [TestMethod]
        public void TestVideoProfile()
        {
            var profile = new AndroidProfile(true, false, new[]
            {
                "high",
                "baseline",
                "constrained baseline"
            });

            var builder = new StreamBuilder();

            var mediaSources = new List<MediaSourceInfo>
            {
                new MediaSourceInfo
                {
                      Bitrate = 6200000,
                      Container = "mkv",
                      Path= "\\server\\test.mkv",
                      Protocol = Model.MediaInfo.MediaProtocol.File,
                      RunTimeTicks = TimeSpan.FromMinutes(60).Ticks,
                      VideoType = VideoType.VideoFile,
                      Type = MediaSourceType.Default,
                      MediaStreams = new List<MediaStream>
                      {
                          new MediaStream
                          {
                              Codec = "H264",
                              Type = MediaStreamType.Video,
                              Profile = "High",
                              IsCabac = true
                          },
                          new MediaStream
                          {
                              Codec = "AC3",
                              Type = MediaStreamType.Audio
                          }
                      }
                }
            };

            var options = new VideoOptions
            {
                Context = EncodingContext.Streaming,
                DeviceId = Guid.NewGuid().ToString(),
                ItemId = Guid.NewGuid().ToString(),
                Profile = profile,
                MediaSources = mediaSources
            };

            var streamInfo = builder.BuildVideoItem(options);

            var url = streamInfo.ToDlnaUrl("http://localhost:8096");

            var containsHighProfile = url.IndexOf(";high;", StringComparison.OrdinalIgnoreCase) != -1;
            var containsBaseline = url.IndexOf(";baseline;", StringComparison.OrdinalIgnoreCase) != -1;

            Assert.IsTrue(containsHighProfile);
            Assert.IsFalse(containsBaseline);

            var isHls = url.IndexOf("master.m3u8?", StringComparison.OrdinalIgnoreCase) != -1;
            Assert.IsTrue(isHls);
        }
    }
}
