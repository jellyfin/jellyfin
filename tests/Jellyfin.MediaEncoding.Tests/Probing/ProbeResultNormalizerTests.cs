using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing
{
    public class ProbeResultNormalizerTests
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ProbeResultNormalizer _probeResultNormalizer = new ProbeResultNormalizer(new NullLogger<EncoderValidatorTests>(), new Mock<ILocalizationManager>().Object);

        public ProbeResultNormalizerTests()
        {
            _jsonOptions = new JsonSerializerOptions(JsonDefaults.Options);
            _jsonOptions.Converters.Add(new JsonBoolStringConverter());
        }

        [Theory]
        [InlineData("2997/125", 23.976f)]
        [InlineData("1/50", 0.02f)]
        [InlineData("25/1", 25f)]
        [InlineData("120/1", 120f)]
        [InlineData("1704753000/71073479", 23.98578237601117f)]
        [InlineData("0/0", null)]
        [InlineData("1/1000", 0.001f)]
        [InlineData("1/90000", 1.1111111E-05f)]
        [InlineData("1/48000", 2.0833333E-05f)]
        public void GetFrameRate_Success(string value, float? expected)
            => Assert.Equal(expected, ProbeResultNormalizer.GetFrameRate(value));

        [Fact]
        public void GetMediaInfo_MetaData_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_metadata.mkv", MediaProtocol.File);

            Assert.Equal("mkv", res.Container);

            Assert.Equal(3, res.MediaStreams.Count);

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
            Assert.False(res.VideoStream.IsHearingImpaired);
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
            Assert.Equal(1, res.VideoStream.DvVersionMajor);
            Assert.Equal(0, res.VideoStream.DvVersionMinor);
            Assert.Equal(5, res.VideoStream.DvProfile);
            Assert.Equal(6, res.VideoStream.DvLevel);
            Assert.Equal(1, res.VideoStream.RpuPresentFlag);
            Assert.Equal(0, res.VideoStream.ElPresentFlag);
            Assert.Equal(1, res.VideoStream.BlPresentFlag);
            Assert.Equal(0, res.VideoStream.DvBlSignalCompatibilityId);
            Assert.Equal(-180, res.VideoStream.Rotation);

            var audio1 = res.MediaStreams[1];
            Assert.Equal("eac3", audio1.Codec);
            Assert.Equal(AudioSpatialFormat.DolbyAtmos, audio1.AudioSpatialFormat);

            var audio2 = res.MediaStreams[2];
            Assert.Equal("dts", audio2.Codec);
            Assert.Equal(AudioSpatialFormat.DTSX, audio2.AudioSpatialFormat);

            Assert.Empty(res.Chapters);
            Assert.Equal("Just color bars", res.Overview);
        }

        [Fact]
        public void GetMediaInfo_Mp4MetaData_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_mp4_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);

            // subtitle handling requires a localization object, set a mock to return the input string
            var mockLocalization = new Mock<ILocalizationManager>();
            mockLocalization.Setup(x => x.GetLocalizedString(It.IsAny<string>())).Returns<string>(x => x);
            ProbeResultNormalizer localizedProbeResultNormalizer = new ProbeResultNormalizer(new NullLogger<EncoderValidatorTests>(), mockLocalization.Object);

            MediaInfo res = localizedProbeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_mp4_metadata.mkv", MediaProtocol.File);

            // [Video, Audio (Main), Audio (Commentary), Subtitle (Main, Spanish), Subtitle (Main, English), Subtitle (Commentary)
            Assert.Equal(6, res.MediaStreams.Count);

            Assert.NotNull(res.VideoStream);
            Assert.Equal(res.MediaStreams[0], res.VideoStream);
            Assert.Equal(0, res.VideoStream.Index);
            Assert.Equal("h264", res.VideoStream.Codec);
            Assert.Equal("High", res.VideoStream.Profile);
            Assert.Equal(MediaStreamType.Video, res.VideoStream.Type);
            Assert.Equal(358, res.VideoStream.Height);
            Assert.Equal(720, res.VideoStream.Width);
            Assert.Equal("2.40:1", res.VideoStream.AspectRatio);
            Assert.Equal("yuv420p", res.VideoStream.PixelFormat);
            Assert.Equal(31d, res.VideoStream.Level);
            Assert.Equal(1, res.VideoStream.RefFrames);
            Assert.True(res.VideoStream.IsAVC);
            Assert.Equal(120f, res.VideoStream.RealFrameRate);
            Assert.Equal("1/90000", res.VideoStream.TimeBase);
            Assert.Equal(1147365, res.VideoStream.BitRate);
            Assert.Equal(8, res.VideoStream.BitDepth);
            Assert.True(res.VideoStream.IsDefault);
            Assert.Equal("und", res.VideoStream.Language);

            Assert.Equal(MediaStreamType.Audio, res.MediaStreams[1].Type);
            Assert.Equal("aac", res.MediaStreams[1].Codec);
            Assert.Equal(7, res.MediaStreams[1].Channels);
            Assert.True(res.MediaStreams[1].IsDefault);
            Assert.Equal("eng", res.MediaStreams[1].Language);
            Assert.Equal("Surround 6.1", res.MediaStreams[1].Title);

            Assert.Equal(MediaStreamType.Audio, res.MediaStreams[2].Type);
            Assert.Equal("aac", res.MediaStreams[2].Codec);
            Assert.Equal(2, res.MediaStreams[2].Channels);
            Assert.False(res.MediaStreams[2].IsDefault);
            Assert.Equal("eng", res.MediaStreams[2].Language);
            Assert.Equal("Commentary", res.MediaStreams[2].Title);

            Assert.Equal("spa", res.MediaStreams[3].Language);
            Assert.Equal(MediaStreamType.Subtitle, res.MediaStreams[3].Type);
            Assert.Equal("DVDSUB", res.MediaStreams[3].Codec);
            Assert.Null(res.MediaStreams[3].Title);
            Assert.False(res.MediaStreams[3].IsHearingImpaired);

            Assert.Equal("eng", res.MediaStreams[4].Language);
            Assert.Equal(MediaStreamType.Subtitle, res.MediaStreams[4].Type);
            Assert.Equal("mov_text", res.MediaStreams[4].Codec);
            Assert.Null(res.MediaStreams[4].Title);
            Assert.True(res.MediaStreams[4].IsHearingImpaired);

            Assert.Equal("eng", res.MediaStreams[5].Language);
            Assert.Equal(MediaStreamType.Subtitle, res.MediaStreams[5].Type);
            Assert.Equal("mov_text", res.MediaStreams[5].Codec);
            Assert.Equal("Commentary", res.MediaStreams[5].Title);
            Assert.False(res.MediaStreams[5].IsHearingImpaired);
        }

        [Fact]
        public void GetMediaInfo_TS_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_ts.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);

            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_metadata.mkv", MediaProtocol.File);

            Assert.Equal(2, res.MediaStreams.Count);

            Assert.False(res.MediaStreams[0].IsAVC);
        }

        [Fact]
        public void GetMediaInfo_WebM_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_webm.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);

            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_metadata.webm", MediaProtocol.File);

            Assert.Equal("mkv,webm", res.Container);

            Assert.Equal(2, res.MediaStreams.Count);

            Assert.False(res.MediaStreams[0].IsAVC);
        }

        [Fact]
        public void GetMediaInfo_ProgressiveVideoNoFieldOrder_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_progressive_no_field_order.json");

            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_progressive_no_field_order.mp4", MediaProtocol.File);

            Assert.Equal(2, res.MediaStreams.Count);

            Assert.NotNull(res.VideoStream);
            Assert.Equal(res.MediaStreams[0], res.VideoStream);
            Assert.Equal(0, res.VideoStream.Index);
            Assert.Equal("h264", res.VideoStream.Codec);
            Assert.Equal("Main", res.VideoStream.Profile);
            Assert.Equal(MediaStreamType.Video, res.VideoStream.Type);
            Assert.Equal(1080, res.VideoStream.Height);
            Assert.Equal(1920, res.VideoStream.Width);
            Assert.False(res.VideoStream.IsInterlaced);
            Assert.Equal("16:9", res.VideoStream.AspectRatio);
            Assert.Equal("yuv420p", res.VideoStream.PixelFormat);
            Assert.Equal(41d, res.VideoStream.Level);
            Assert.Equal(1, res.VideoStream.RefFrames);
            Assert.True(res.VideoStream.IsAVC);
            Assert.Equal(23.9760246f, res.VideoStream.RealFrameRate);
            Assert.Equal("1/24000", res.VideoStream.TimeBase);
            Assert.Equal(3948341, res.VideoStream.BitRate);
            Assert.Equal(8, res.VideoStream.BitDepth);
            Assert.True(res.VideoStream.IsDefault);
        }

        [Fact]
        public void GetMediaInfo_ProgressiveVideoNoFieldOrder2_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_progressive_no_field_order2.json");

            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_progressive_no_field_order2.mp4", MediaProtocol.File);

            Assert.Single(res.MediaStreams);

            Assert.NotNull(res.VideoStream);
            Assert.Equal(res.MediaStreams[0], res.VideoStream);
            Assert.Equal(0, res.VideoStream.Index);
            Assert.Equal("h264", res.VideoStream.Codec);
            Assert.Equal("High", res.VideoStream.Profile);
            Assert.Equal(MediaStreamType.Video, res.VideoStream.Type);
            Assert.Equal(720, res.VideoStream.Height);
            Assert.Equal(1280, res.VideoStream.Width);
            Assert.False(res.VideoStream.IsInterlaced);
            Assert.Equal("16:9", res.VideoStream.AspectRatio);
            Assert.Equal("yuv420p", res.VideoStream.PixelFormat);
            Assert.Equal(31d, res.VideoStream.Level);
            Assert.Equal(1, res.VideoStream.RefFrames);
            Assert.True(res.VideoStream.IsAVC);
            Assert.Equal(25f, res.VideoStream.RealFrameRate);
            Assert.Equal("1/12800", res.VideoStream.TimeBase);
            Assert.Equal(53288, res.VideoStream.BitRate);
            Assert.Equal(8, res.VideoStream.BitDepth);
            Assert.True(res.VideoStream.IsDefault);
        }

        [Fact]
        public void GetMediaInfo_InterlacedVideo_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/video_interlaced.json");

            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, VideoType.VideoFile, false, "Test Data/Probing/video_interlaced.mp4", MediaProtocol.File);

            Assert.Single(res.MediaStreams);

            Assert.NotNull(res.VideoStream);
            Assert.Equal(res.MediaStreams[0], res.VideoStream);
            Assert.Equal(0, res.VideoStream.Index);
            Assert.Equal("h264", res.VideoStream.Codec);
            Assert.Equal("High", res.VideoStream.Profile);
            Assert.Equal(MediaStreamType.Video, res.VideoStream.Type);
            Assert.Equal(720, res.VideoStream.Height);
            Assert.Equal(1280, res.VideoStream.Width);
            Assert.True(res.VideoStream.IsInterlaced);
            Assert.Equal("16:9", res.VideoStream.AspectRatio);
            Assert.Equal("yuv420p", res.VideoStream.PixelFormat);
            Assert.Equal(40d, res.VideoStream.Level);
            Assert.Equal(1, res.VideoStream.RefFrames);
            Assert.True(res.VideoStream.IsAVC);
            Assert.Equal(25f, res.VideoStream.RealFrameRate);
            Assert.Equal("1/12800", res.VideoStream.TimeBase);
            Assert.Equal(56945, res.VideoStream.BitRate);
            Assert.Equal(8, res.VideoStream.BitDepth);
            Assert.True(res.VideoStream.IsDefault);
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
            Assert.Equal(DateTime.Parse("2021-01-01T00:00Z", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AdjustToUniversal), res.PremiereDate);
        }

        [Fact]
        public void GetMediaInfo_GivenOriginalDateContainsOnlyYear_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/music_year_only_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, null, true, "Test Data/Probing/music.flac", MediaProtocol.File);

            Assert.Equal("Baker Street", res.Name);
            Assert.Single(res.Artists);
            Assert.Equal("Gerry Rafferty", res.Artists[0]);
            Assert.Equal("City to City", res.Album);
            Assert.Equal(1978, res.ProductionYear);
            Assert.True(res.PremiereDate.HasValue);
            Assert.Equal(DateTime.Parse("1978-01-01T00:00Z", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AdjustToUniversal), res.PremiereDate);
            Assert.Contains("Electronic", res.Genres);
            Assert.Contains("Ambient", res.Genres);
            Assert.Contains("Pop", res.Genres);
            Assert.Contains("Jazz", res.Genres);
        }

        [Fact]
        public void GetMediaInfo_Music_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/Probing/music_metadata.json");
            var internalMediaInfoResult = JsonSerializer.Deserialize<InternalMediaInfoResult>(bytes, _jsonOptions);
            MediaInfo res = _probeResultNormalizer.GetMediaInfo(internalMediaInfoResult, null, true, "Test Data/Probing/music.flac", MediaProtocol.File);

            Assert.Equal("UP NO MORE", res.Name);
            Assert.Single(res.Artists);
            Assert.Equal("TWICE", res.Artists[0]);
            Assert.Equal("Eyes wide open", res.Album);
            Assert.Equal(2020, res.ProductionYear);
            Assert.True(res.PremiereDate.HasValue);
            Assert.Equal(DateTime.Parse("2020-10-26T00:00Z", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AdjustToUniversal), res.PremiereDate);
            Assert.Equal(22, res.People.Length);
            Assert.Equal("Krysta Youngs", res.People[0].Name);
            Assert.Equal(PersonKind.Composer, res.People[0].Type);
            Assert.Equal("Julia Ross", res.People[1].Name);
            Assert.Equal(PersonKind.Composer, res.People[1].Type);
            Assert.Equal("Yiwoomin", res.People[2].Name);
            Assert.Equal(PersonKind.Composer, res.People[2].Type);
            Assert.Equal("Ji-hyo Park", res.People[3].Name);
            Assert.Equal(PersonKind.Lyricist, res.People[3].Type);
            Assert.Equal("Yiwoomin", res.People[4].Name);
            Assert.Equal(PersonKind.Actor, res.People[4].Type);
            Assert.Equal("Electric Piano", res.People[4].Role);
            Assert.Equal(4, res.Genres.Length);
            Assert.Contains("Electronic", res.Genres);
            Assert.Contains("Trance", res.Genres);
            Assert.Contains("Dance", res.Genres);
            Assert.Contains("Jazz", res.Genres);
        }
    }
}
