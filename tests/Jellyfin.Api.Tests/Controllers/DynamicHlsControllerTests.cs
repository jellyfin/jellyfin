using System;
using System.Globalization;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class DynamicHlsControllerTests
    {
        private readonly Mock<IMediaEncoder> _mediaEncoder;

        public DynamicHlsControllerTests()
        {
            _mediaEncoder = new Mock<IMediaEncoder>();
            _mediaEncoder
                .Setup(e => e.GetTimeParameter(It.IsAny<long>()))
                .Returns((long ticks) => TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(GetSegmentLengths_Success_TestData))]
        public void GetSegmentLengths_Success(long runtimeTicks, int segmentlength, double[] expected)
        {
            var res = DynamicHlsController.GetSegmentLengthsInternal(runtimeTicks, segmentlength);
            Assert.Equal(expected.Length, res.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], res[i]);
            }
        }

        public static TheoryData<long, int, double[]> GetSegmentLengths_Success_TestData()
        {
            var data = new TheoryData<long, int, double[]>();
            data.Add(0, 6, Array.Empty<double>());
            data.Add(
                TimeSpan.FromSeconds(3).Ticks,
                6,
                new double[] { 3 });
            data.Add(
                TimeSpan.FromSeconds(6).Ticks,
                6,
                new double[] { 6 });
            data.Add(
                TimeSpan.FromSeconds(3.3333333).Ticks,
                6,
                new double[] { 3.3333333 });
            data.Add(
                TimeSpan.FromSeconds(9.3333333).Ticks,
                6,
                new double[] { 6, 3.3333333 });

            return data;
        }

        [Theory]
        [InlineData(600000000L, true, "libx264", "copy", HlsAudioSeekStrategy.OutputSeek, " -ss 00:01:00.000")] // OutputSeek + video transcode + audio copy → trim
        [InlineData(600000000L, true, "libx264", "copy", HlsAudioSeekStrategy.DisableAccurateSeek, "")] // DisableAccurateSeek → no trim
        [InlineData(600000000L, true, "libx264", "copy", HlsAudioSeekStrategy.TranscodeAudio, "")] // TranscodeAudio → no trim
        [InlineData(600000000L, true, "libx264", "aac", HlsAudioSeekStrategy.OutputSeek, "")] // both transcode → no trim
        [InlineData(600000000L, true, "copy", "copy", HlsAudioSeekStrategy.OutputSeek, "")] // both copy → no trim
        [InlineData(0L, true, "libx264", "copy", HlsAudioSeekStrategy.OutputSeek, "")] // zero start time → no trim
        [InlineData(600000000L, false, "libx264", "copy", HlsAudioSeekStrategy.OutputSeek, "")] // audio-only → no trim
        public void GetOutputSeekParam_ReturnsExpected(long startTimeTicks, bool isOutputVideo, string videoCodec, string audioCodec, HlsAudioSeekStrategy strategy, string expected)
        {
            var result = DynamicHlsController.GetOutputSeekParam(
                startTimeTicks,
                isOutputVideo,
                videoCodec,
                audioCodec,
                _mediaEncoder.Object,
                strategy);

            Assert.Equal(expected, result);
        }
    }
}
