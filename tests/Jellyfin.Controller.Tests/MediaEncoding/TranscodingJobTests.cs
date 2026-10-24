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
    public void GetHighestServedSegmentIndexEndingAtOrBefore_NoSegmentsServed_ReturnsNull()
    {
        using var job = CreateJob();

        Assert.Null(job.GetHighestServedSegmentIndexEndingAtOrBefore(TimeSpan.FromHours(1).Ticks));
    }

    [Fact]
    public void GetHighestServedSegmentIndexEndingAtOrBefore_InitSegment_IsIgnored()
    {
        using var job = CreateJob();
        job.ReportSegmentDownloaded(-1, 0);

        Assert.Null(job.GetHighestServedSegmentIndexEndingAtOrBefore(TimeSpan.FromHours(1).Ticks));
    }

    [Fact]
    public void GetHighestServedSegmentIndexEndingAtOrBefore_SegmentEndingExactlyAtPosition_IsIncluded()
    {
        using var job = CreateJob();
        job.ReportSegmentDownloaded(0, TimeSpan.FromSeconds(6).Ticks);
        job.ReportSegmentDownloaded(1, TimeSpan.FromSeconds(12).Ticks);

        Assert.Equal(1, job.GetHighestServedSegmentIndexEndingAtOrBefore(TimeSpan.FromSeconds(12).Ticks));
        Assert.Equal(0, job.GetHighestServedSegmentIndexEndingAtOrBefore(TimeSpan.FromSeconds(11.9).Ticks));
    }

    [Fact]
    public void GetHighestServedSegmentIndexEndingAtOrBefore_SegmentsLongerThanDesired_ReturnsServedIndex()
    {
        // Keyframe-based playlists average longer segments than the desired 6s.
        const double SegmentSeconds = 6.6;
        using var job = CreateJob();
        for (var i = 0; i < 520; i++)
        {
            job.ReportSegmentDownloaded(i, TimeSpan.FromSeconds((i + 1) * SegmentSeconds).Ticks);
        }

        var keepFrom = TimeSpan.FromSeconds((520 * SegmentSeconds) - 120);

        Assert.Equal(500, job.GetHighestServedSegmentIndexEndingAtOrBefore(keepFrom.Ticks));
    }
}
