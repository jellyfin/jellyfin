using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Hls.Playlist;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class DynamicHlsControllerTests
    {
        [Theory]
        [InlineData(8, 1, "smpte2084", false, "hvc1")]
        [InlineData(8, 1, "smpte2084", true, "hvc1")]
        [InlineData(8, 2, "bt709", false, "hvc1")]
        [InlineData(8, 4, "arib-std-b67", false, "hvc1")]
        [InlineData(8, 6, "smpte2084", false, "hvc1")]
        [InlineData(5, 0, "smpte2084", false, "dvh1")]
        [InlineData(7, 6, "smpte2084", false, "dvh1")]
        [InlineData(null, null, "smpte2084", false, "dvh1")]
        public void GetDolbyVisionHevcCodecTag_SelectsTagForProfile(int? profile, int? compatibilityId, string transfer, bool hdr10Plus, string expected)
        {
            var stream = new MediaStream
            {
                Codec = "hevc",
                Type = MediaStreamType.Video,
                RpuPresentFlag = 1,
                BlPresentFlag = 1,
                ColorSpace = "bt2020nc",
                ColorPrimaries = "bt2020",
                ColorTransfer = transfer,
                DvProfile = profile,
                DvBlSignalCompatibilityId = compatibilityId,
                Hdr10PlusPresentFlag = hdr10Plus
            };

            Assert.Equal(expected, DynamicHlsController.GetDolbyVisionHevcCodecTag(stream));
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
        [InlineData(2, 2)] // mid-file: the run starts on the requested segment
        [InlineData(3, 3)] // starts exactly at runtime - 5 s: the clamped seek still reaches it
        [InlineData(4, 3)] // starts inside the final 5 s: the clamped seek lands before it, so the run starts one segment earlier
        public void GetSeekableSegment_ClampedSeek_StartsOnReachableSegment(int segmentId, int expected)
        {
            // Segment starts at 0, 60, 120, 175 and 178 s of a 180 s file
            long[] segmentStarts = [0, 600_000_000, 1_200_000_000, 1_750_000_000, 1_780_000_000];

            Assert.Equal(expected, DynamicHlsController.GetSeekableSegment(segmentStarts, segmentId, 1_800_000_000));
        }

        [Fact]
        public void GetSeekableSegment_UnknownRuntime_StartsOnRequestedSegment()
        {
            // The seek is not clamped when the runtime is unknown, so every segment stays reachable
            long[] segmentStarts = [0, 600_000_000, 1_200_000_000, 1_750_000_000, 1_780_000_000];

            Assert.Equal(4, DynamicHlsController.GetSeekableSegment(segmentStarts, 4, 0));
        }

        [Fact]
        public void GetSegmentCutArguments_RelativeToStart_FormatsInvariantly()
        {
            // Segment starts at 0, 165.415, 175.592 and 180 s; the run starts at the second one
            long[] segmentStarts = [0, 1_654_150_000, 1_755_920_000, 1_800_000_000];
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                Assert.Equal("-segment_times 10.177,14.585 -segment_time_delta 0.5", DynamicHlsController.GetSegmentCutArguments(segmentStarts, 1));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public void GetSegmentCutArguments_NothingLeftToCut_UsesSegmentTime()
        {
            long[] segmentStarts = [0, 1_654_150_000, 1_755_920_000];

            Assert.Equal("-segment_time 86400", DynamicHlsController.GetSegmentCutArguments(segmentStarts, 2));
        }

        [Fact]
        public void GetSegmentCutArguments_SeveralHours_LongerThanWindowsCommandLine()
        {
            // A 6 hour file with 6.123 s segments: the list from the first segment does not fit in a Windows command line, the one for its last hour does
            var segmentStarts = Enumerable.Range(0, 3528).Select(i => i * 61_234_567L).ToArray();

            Assert.True(DynamicHlsController.GetSegmentCutArguments(segmentStarts, 0).Length > 24_000);
            Assert.True(DynamicHlsController.GetSegmentCutArguments(segmentStarts, 2940).Length <= 24_000);
            Assert.True(DynamicHlsController.MaxSegmentCutArgumentsLength >= 24_000);
        }

        [Fact]
        public void GetSegmentStartTicks_MatchesPlaylistRuntimeTicks()
        {
            // Uneven keyframe gaps with odd tick counts, so the segment lengths have fractional ticks that must round the way the playlist does
            long[] gaps = [23_560_001, 34_560_002, 13_780_004, 28_140_002, 17_960_002, 27_330_002];
            var keyframeTicks = new List<long>();
            for (long tick = 0, i = 0; tick < 1_800_000_000; tick += gaps[i++ % gaps.Length])
            {
                keyframeTicks.Add(tick);
            }

            KeyframeData? keyframes = new KeyframeData(1_800_000_000, keyframeTicks);
            var extractor = new Mock<IKeyframeExtractor>();
            extractor.SetupGet(e => e.IsMetadataBased).Returns(true);
            extractor.Setup(e => e.TryExtractKeyframes(It.IsAny<Guid>(), It.IsAny<string>(), out keyframes)).Returns(true);
            var config = new Mock<IServerConfigurationManager>();
            config.Setup(c => c.GetConfiguration("encoding"))
                .Returns(new EncodingOptions { AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = ["mkv"] });
            var generator = new DynamicHlsPlaylistGenerator(config.Object, [extractor.Object]);
            var request = new CreateMainPlaylistRequest(Guid.NewGuid(), "/media/film.mkv", 6000, 1_800_000_000, "mp4", "hls1/main/", "?a=b", true);

            var advertised = generator.CreateMainPlaylist(request)
                .Split('\n')
                .Where(line => line.Length > 0 && line[0] != '#')
                .Select(line => long.Parse(Regex.Match(line, @"runtimeTicks=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture))
                .ToArray();
            Assert.True(generator.TryGetKeyframeSegmentLengths(request, out var lengths));
            var computed = DynamicHlsController.GetSegmentStartTicks(lengths);

            // The keyframe path, not the even 6 s grid, is what makes the rounding matter
            Assert.Contains(lengths, length => Math.Abs(length - 6) > 0.001);
            Assert.Equal(advertised, computed);
        }

        [Fact]
        public async Task WaitForActiveTranscodingRequests_WaitsUntilRequestCompletes()
        {
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
            {
                ActiveRequestCount = 1
            };

            var waitTask = DynamicHlsController.WaitForActiveTranscodingRequests(job, CancellationToken.None);
            Assert.False(waitTask.IsCompleted);

            job.DecrementActiveRequestCount();

            await waitTask;
        }

        [Fact]
        public async Task WaitForActiveTranscodingRequests_WaitsForEveryRequest()
        {
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
            {
                ActiveRequestCount = 2
            };

            var waitTask = DynamicHlsController.WaitForActiveTranscodingRequests(job, CancellationToken.None);
            job.DecrementActiveRequestCount();

            await Task.Delay(150, TestContext.Current.CancellationToken);
            Assert.False(waitTask.IsCompleted);

            job.DecrementActiveRequestCount();

            await waitTask;
        }

        [Fact]
        public async Task WaitForActiveTranscodingRequests_ReturnsWithoutAnActiveRequest()
        {
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance);

            await DynamicHlsController.WaitForActiveTranscodingRequests(job, CancellationToken.None);
            await DynamicHlsController.WaitForActiveTranscodingRequests(null, CancellationToken.None);
        }

        [Fact]
        public async Task WaitForActiveTranscodingRequests_ObservesCancellation()
        {
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
            {
                ActiveRequestCount = 1
            };
            using var cancellationTokenSource = new CancellationTokenSource();

            var waitTask = DynamicHlsController.WaitForActiveTranscodingRequests(job, cancellationTokenSource.Token);
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
        }

        [Fact]
        public async Task ActiveRequestCount_UpdatesAtomically()
        {
            const int RequestCount = 1000;
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance);

            await Task.WhenAll(
                Task.Run(() => Parallel.For(0, RequestCount, _ => job.IncrementActiveRequestCount())),
                Task.Run(() => Parallel.For(0, RequestCount, _ => job.DecrementActiveRequestCount())));

            Assert.Equal(0, job.ActiveRequestCount);
        }
    }
}
