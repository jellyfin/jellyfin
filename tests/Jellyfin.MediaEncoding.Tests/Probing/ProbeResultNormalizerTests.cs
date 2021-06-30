using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing
{
    public class ProbeResultNormalizerTests
    {
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private readonly ProbeResultNormalizer _probeResultNormalizer = new ProbeResultNormalizer(new NullLogger<EncoderValidatorTests>(), null);

        [Fact]
        public void GetMediaInfo_MetaData_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_metadata.mkv", MediaProtocol.File);

            Assert.Single(res.MediaStreams);

            Assert.NotNull(res.VideoStream);
            Assert.Equal("4:3", res.VideoStream.AspectRatio);
            Assert.Equal(25f, res.VideoStream.AverageFrameRate);
            Assert.Equal(8, res.VideoStream.BitDepth);
            Assert.Equal(69432, res.VideoStream.BitRate);
            Assert.Equal("h264", res.VideoStream.Codec);
            Assert.Equal("1/50", res.VideoStream.CodecTimeBase);
            Assert.Equal(240, res.VideoStream.Height);
            Assert.Equal(320, res.VideoStream.Width);
            Assert.Equal(0, res.VideoStream.Index);
            Assert.False(res.VideoStream.IsAnamorphic);
            Assert.True(res.VideoStream.IsAVC);
            Assert.True(res.VideoStream.IsDefault);
            Assert.False(res.VideoStream.IsExternal);
            Assert.False(res.VideoStream.IsForced);
            Assert.False(res.VideoStream.IsInterlaced);
            Assert.False(res.VideoStream.IsTextSubtitleStream);
            Assert.Equal(13d, res.VideoStream.Level);
            Assert.Equal("4", res.VideoStream.NalLengthSize);
            Assert.Equal("yuv444p", res.VideoStream.PixelFormat);
            Assert.Equal("High 4:4:4 Predictive", res.VideoStream.Profile);
            Assert.Equal(25f, res.VideoStream.RealFrameRate);
            Assert.Equal(1, res.VideoStream.RefFrames);
            Assert.Equal("1/1000", res.VideoStream.TimeBase);
            Assert.Equal(MediaStreamType.Video, res.VideoStream.Type);

            Assert.Empty(res.Chapters);
            Assert.Equal("Just color bars", res.Overview);
        }

        [Fact]
        public void GetMediaInfo_MusicVideo_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/music_video_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/music_video.mkv", MediaProtocol.File);

            Assert.Equal("The Title", res.Name);
            Assert.Equal("Title, The", res.ForcedSortName);
            Assert.Single(res.Artists);
            Assert.Equal("The Artist", res.Artists[0]);
            Assert.Equal("Album", res.Album);
            Assert.Equal(2021, res.ProductionYear);
            Assert.True(res.PremiereDate.HasValue);
            Assert.Equal(DateTime.Parse("2021-01-01T00:00Z", DateTimeFormatInfo.CurrentInfo).ToUniversalTime(), res.PremiereDate);
        }
    }
}
