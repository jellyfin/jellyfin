using System;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding
{
    public class EncodingHelperAudioBitStreamTests
    {
        private const string BothFilters = " -bsf:a noise=drop='lt(pts*tb\\,63.063)',aac_adtstoasc";
        private const string NoiseOnly = " -bsf:a noise=drop='lt(pts*tb\\,63.063)'";
        private const string AdtsOnly = " -bsf:a aac_adtstoasc";
        private const long DefaultSeekTicks = 630_630_000L;
        private const string DefaultFfmpegVersion = "7.0.1";

        private static EncodingHelper CreateHelper(Version encoderVersion)
        {
            var mediaEncoder = new Mock<IMediaEncoder>();
            mediaEncoder
                .Setup(e => e.GetTimeParameter(It.IsAny<long>()))
                .Returns((long ticks) => TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
            mediaEncoder
                .SetupGet(e => e.EncoderVersion)
                .Returns(encoderVersion);

            return new EncodingHelper(
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
            string audioStreamCodec,
            string inputContainer,
            long startTimeTicks)
        {
            return new EncodingJobInfo(jobType)
            {
                IsVideoRequest = true,
                OutputVideoCodec = outputVideoCodec,
                OutputAudioCodec = outputAudioCodec,
                InputContainer = inputContainer,
                RunTimeTicks = TimeSpan.FromMinutes(10).Ticks,
                AudioStream = new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = audioStreamCodec
                },
                BaseRequest = new BaseEncodingJobOptions
                {
                    StartTimeTicks = startTimeTicks
                }
            };
        }

        [Theory]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", BothFilters)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "aac", BothFilters)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "hls", BothFilters)]
        [InlineData(TranscodingJobType.Progressive, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "copy", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "aac", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "wtv", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", 0L, DefaultFfmpegVersion, "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, "6.1.1", "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, "6.0", "mp4", "ts", AdtsOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "ts", "ts", NoiseOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "aac", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "mkv", NoiseOnly)]
        [InlineData(TranscodingJobType.Hls, "libx264", "copy", "ac3", "ts", DefaultSeekTicks, DefaultFfmpegVersion, "mp4", "ts", NoiseOnly)]
        public void AudioBitStreamArguments_AppliesGates(
            TranscodingJobType jobType,
            string outputVideoCodec,
            string outputAudioCodec,
            string audioStreamCodec,
            string inputContainer,
            long startTicks,
            string ffmpegVersion,
            string segmentContainer,
            string mediaSourceContainer,
            string expected)
        {
            var state = CreateState(jobType, outputVideoCodec, outputAudioCodec, audioStreamCodec, inputContainer, startTicks);
            var result = CreateHelper(Version.Parse(ffmpegVersion)).GetAudioBitStreamArguments(state, segmentContainer, mediaSourceContainer);
            Assert.Equal(expected, result);
        }
    }
}
