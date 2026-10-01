using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests;

public class EncodingJobInfoOutputDimsTests
{
    [Fact]
    public void OutputWidth_ResolvedSet_OverridesLegacyMath()
    {
        var state = BuildState(srcW: 1920, srcH: 1080, maxW: 720, maxH: null);
        state.ResolvedOutputWidth = 720;
        state.ResolvedOutputHeight = 404;

        Assert.Equal(720, state.OutputWidth);
        Assert.Equal(404, state.OutputHeight);
    }

    [Fact]
    public void OutputWidth_ResolvedOnlyWidth_HeightFallsBackToLegacy()
    {
        var state = BuildState(srcW: 1920, srcH: 1080, maxW: 720, maxH: null);
        state.ResolvedOutputWidth = 998;

        Assert.Equal(998, state.OutputWidth);
        Assert.NotEqual(998, state.OutputHeight);
    }

    [Fact]
    public void OutputWidth_ResolvedClearedToNull_ReturnsLegacyAgain()
    {
        var state = BuildState(srcW: 1920, srcH: 1080, maxW: 720, maxH: null);
        state.ResolvedOutputWidth = 998;
        var resolvedValue = state.OutputWidth;

        state.ResolvedOutputWidth = null;
        var legacyValue = state.OutputWidth;

        Assert.Equal(998, resolvedValue);
        Assert.NotEqual(998, legacyValue);
    }

    private static EncodingJobInfo BuildState(int srcW, int srcH, int? maxW, int? maxH)
    {
        return new EncodingJobInfo(TranscodingJobType.Hls)
        {
            IsVideoRequest = true,
            BaseRequest = new BaseEncodingJobOptions
            {
                MaxWidth = maxW,
                MaxHeight = maxH,
            },
            VideoStream = new MediaStream
            {
                Type = MediaStreamType.Video,
                Width = srcW,
                Height = srcH,
            },
        };
    }
}
