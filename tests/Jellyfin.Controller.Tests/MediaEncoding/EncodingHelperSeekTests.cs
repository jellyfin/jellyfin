using System;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding
{
    public class EncodingHelperSeekTests
    {
        private readonly EncodingHelper _helper;
        private readonly EncodingOptions _encodingOptions = new();

        public EncodingHelperSeekTests()
        {
            var mediaEncoder = new Mock<IMediaEncoder>();
            mediaEncoder
                .Setup(e => e.GetTimeParameter(It.IsAny<long>()))
                .Returns((long ticks) => TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
            mediaEncoder
                .SetupGet(e => e.EncoderVersion)
                .Returns(new Version(7, 0, 1));

            _helper = new EncodingHelper(
                Mock.Of<IApplicationPaths>(),
                mediaEncoder.Object,
                Mock.Of<ISubtitleEncoder>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<IConfigurationManager>(),
                Mock.Of<IPathManager>());
        }

        private static EncodingJobInfo CreateState(
            TranscodingJobType jobType,
            string outputVideoCodec,
            string outputAudioCodec,
            long? startTimeTicks = null,
            string inputContainer = "mkv")
        {
            return new EncodingJobInfo(jobType)
            {
                IsVideoRequest = true,
                OutputVideoCodec = outputVideoCodec,
                OutputAudioCodec = outputAudioCodec,
                InputContainer = inputContainer,
                RunTimeTicks = TimeSpan.FromMinutes(10).Ticks,
                BaseRequest = new BaseEncodingJobOptions
                {
                    StartTimeTicks = startTimeTicks
                }
            };
        }

        [Theory]
        [InlineData("ts")]
        [InlineData("mp4")]
        public void HlsTranscodeVideoCopyAudio_AddsBsfDrop(string segmentContainer)
        {
            var seekTicks = TimeSpan.FromSeconds(63.063).Ticks;
            var state = CreateState(TranscodingJobType.Hls, "libx264", "copy", seekTicks);

            var result = _helper.GetFastSeekCommandLineParameter(state, _encodingOptions, segmentContainer);

            Assert.Contains("-ss 00:01:03.063", result, StringComparison.Ordinal);
            Assert.Contains("-bsf:a noise=drop='lt(pts*tb\\,63.063)'", result, StringComparison.Ordinal);
            Assert.DoesNotContain("-noaccurate_seek", result, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(TranscodingJobType.Hls, "libx264", "aac", "ts", "mkv", "-ss 00:01:03.000")]
        [InlineData(TranscodingJobType.Hls, "copy", "copy", "ts", "mkv", "-ss 00:01:03.500")]
        [InlineData(TranscodingJobType.Hls, "copy", "aac", "ts", "mkv", "-ss 00:01:03.500")]
        [InlineData(TranscodingJobType.Progressive, "libx264", "copy", "ts", "mkv", "-ss 00:01:03.000")]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "ts", "wtv", "-ss 00:01:03.000")]
        public void NoBsfWhenConditionsNotMet(
            TranscodingJobType jobType,
            string videoCodec,
            string audioCodec,
            string segmentContainer,
            string inputContainer,
            string expectedSs)
        {
            var seekTicks = TimeSpan.FromSeconds(63).Ticks;
            var state = CreateState(jobType, videoCodec, audioCodec, seekTicks, inputContainer);

            var result = _helper.GetFastSeekCommandLineParameter(state, _encodingOptions, segmentContainer);

            Assert.Contains(expectedSs, result, StringComparison.Ordinal);
            Assert.DoesNotContain("-bsf:a noise=drop=", result, StringComparison.Ordinal);
            Assert.DoesNotContain("-noaccurate_seek", result, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(null)]
        public void NoSeekTime_EmptyResult(long? seekTicks)
        {
            var state = CreateState(TranscodingJobType.Hls, "libx264", "copy", seekTicks);

            var result = _helper.GetFastSeekCommandLineParameter(state, _encodingOptions, "ts");

            Assert.Empty(result.Trim());
        }
    }
}
