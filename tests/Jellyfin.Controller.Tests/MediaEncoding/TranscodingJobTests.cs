using System;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class TranscodingJobTests
{
    private static TranscodingJob CreateJob()
        => new TranscodingJob(NullLogger<TranscodingJob>.Instance);

    [Fact]
    public void GetLastSegmentIndexEndingBefore_NoSegmentsServed_ReturnsNull()
    {
        using var job = CreateJob();

        Assert.Null(job.GetLastSegmentIndexEndingBefore(TimeSpan.FromHours(1).Ticks));
    }

    [Fact]
    public void GetLastSegmentIndexEndingBefore_InitSegment_IsIgnored()
    {
        using var job = CreateJob();
        job.ReportSegmentDownloaded(-1, 0);

        Assert.Null(job.GetLastSegmentIndexEndingBefore(TimeSpan.FromHours(1).Ticks));
    }

    [Fact]
    public void GetLastSegmentIndexEndingBefore_SegmentEndingExactlyAtPosition_IsIncluded()
    {
        using var job = CreateJob();
        job.ReportSegmentDownloaded(0, TimeSpan.FromSeconds(6).Ticks);
        job.ReportSegmentDownloaded(1, TimeSpan.FromSeconds(12).Ticks);

        Assert.Equal(1, job.GetLastSegmentIndexEndingBefore(TimeSpan.FromSeconds(12).Ticks));
        Assert.Equal(0, job.GetLastSegmentIndexEndingBefore(TimeSpan.FromSeconds(11.9).Ticks));
    }

    [Fact]
    public void GetLastSegmentIndexEndingBefore_SegmentsLongerThanDesired_ReturnsServedIndex()
    {
        // Keyframe-based playlists average longer segments than the desired 6s.
        const double SegmentSeconds = 6.6;
        using var job = CreateJob();
        for (var i = 0; i < 520; i++)
        {
            job.ReportSegmentDownloaded(i, TimeSpan.FromSeconds((i + 1) * SegmentSeconds).Ticks);
        }

        var keepFrom = TimeSpan.FromSeconds((520 * SegmentSeconds) - 120);

        Assert.Equal(500, job.GetLastSegmentIndexEndingBefore(keepFrom.Ticks));
    }
}
