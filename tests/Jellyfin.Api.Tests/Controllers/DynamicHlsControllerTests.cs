using System;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class DynamicHlsControllerTests
    {
        [Fact]
        public void UpdateDownloadPosition_SetsPosition_WhenPreviouslyNull()
        {
            var job = new TranscodingJob(new NullLogger<TranscodingJob>());
            var requestedTicks = TimeSpan.FromMinutes(5).Ticks;

            DynamicHlsController.UpdateDownloadPosition(job, requestedTicks);

            Assert.Equal(requestedTicks, job.DownloadPositionTicks);
        }

        [Fact]
        public void UpdateDownloadPosition_AdvancesPosition_WhenRequestIsAhead()
        {
            var job = new TranscodingJob(new NullLogger<TranscodingJob>())
            {
                DownloadPositionTicks = TimeSpan.FromMinutes(2).Ticks
            };
            var requestedTicks = TimeSpan.FromMinutes(5).Ticks;

            DynamicHlsController.UpdateDownloadPosition(job, requestedTicks);

            Assert.Equal(requestedTicks, job.DownloadPositionTicks);
        }

        [Fact]
        public void UpdateDownloadPosition_DoesNotRegress_WhenRequestIsBehind()
        {
            var existingTicks = TimeSpan.FromMinutes(5).Ticks;
            var job = new TranscodingJob(new NullLogger<TranscodingJob>())
            {
                DownloadPositionTicks = existingTicks
            };
            var requestedTicks = TimeSpan.FromMinutes(2).Ticks;

            DynamicHlsController.UpdateDownloadPosition(job, requestedTicks);

            Assert.Equal(existingTicks, job.DownloadPositionTicks);
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
    }
}
