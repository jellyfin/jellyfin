using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperSessionHooksTests
{
    [Fact]
    public void GetAudioFilterParam_NoProvider_DoesNotAddSessionFilter()
    {
        var args = CreateHelper().GetAudioFilterParam(BuildState(), new EncodingOptions());

        Assert.DoesNotContain("volume=0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetAudioFilterParam_Provider_AppendsFilter()
    {
        const string filter = "volume=0:enable='between(t,1,2)'";
        var provider = new Mock<ISessionAudioFilterProvider>();
        provider
            .Setup(p => p.GetAdditionalAudioFilter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
            .Returns(filter);

        var args = CreateHelper(audio: provider.Object).GetAudioFilterParam(BuildState(), new EncodingOptions());

        Assert.Contains("-af \"", args, StringComparison.Ordinal);
        Assert.Contains(filter, args, StringComparison.Ordinal);
    }

    [Fact]
    public void HasSessionAudioFilterForRequest_Provider_ReturnsTrue()
    {
        var provider = new Mock<ISessionAudioFilterProvider>();
        provider
            .Setup(p => p.GetAdditionalAudioFilter("play", "dev", 0))
            .Returns("volume=0");

        Assert.True(CreateHelper(audio: provider.Object).HasSessionAudioFilterForRequest("play", "dev"));
        Assert.False(CreateHelper().HasSessionAudioFilterForRequest("play", "dev"));
    }

    [Fact]
    public void TryGetSessionEditGraph_NoProvider_ReturnsNull()
    {
        Assert.Null(CreateHelper().TryGetSessionEditGraph(BuildState()));
    }

    [Fact]
    public void TryGetSessionEditGraph_Provider_ReturnsGraph()
    {
        var graph = new SessionMediaEditGraph
        {
            FilterComplex = "[0:v]copy[vout];[0:a]anull[aout]",
            VideoMapLabel = "vout",
            AudioMapLabel = "aout"
        };
        var provider = CreateGraphProvider(graph);

        var result = CreateHelper(graph: provider.Object).TryGetSessionEditGraph(BuildState());

        Assert.NotNull(result);
        Assert.Equal(graph.FilterComplex, result.FilterComplex);
        Assert.Equal("vout", result.VideoMapLabel);
        Assert.Equal("aout", result.AudioMapLabel);
    }

    [Fact]
    public void TryGetSessionEditGraph_EmptyFilterComplex_ReturnsNull()
    {
        var provider = CreateGraphProvider(new SessionMediaEditGraph
        {
            FilterComplex = " ",
            VideoMapLabel = "vout"
        });

        Assert.Null(CreateHelper(graph: provider.Object).TryGetSessionEditGraph(BuildState()));
    }

    [Fact]
    public void TryGetSessionEditGraphMapArgs_IncludesFilterComplexAndMaps()
    {
        var provider = CreateGraphProvider(new SessionMediaEditGraph
        {
            FilterComplex = "[0:v]copy[vout];[0:a]anull[aout]",
            VideoMapLabel = "vout",
            AudioMapLabel = "aout"
        });

        var ok = CreateHelper(graph: provider.Object)
            .TryGetSessionEditGraphMapArgs(BuildState(), out var filterComplexArg, out var mapArgs);

        Assert.True(ok);
        Assert.Equal("-filter_complex \"[0:v]copy[vout];[0:a]anull[aout]\"", filterComplexArg);
        Assert.Contains("-map \"[vout]\"", mapArgs, StringComparison.Ordinal);
        Assert.Contains("-map \"[aout]\"", mapArgs, StringComparison.Ordinal);
    }

    [Fact]
    public void HasSessionEditGraphForRequest_Provider_ReturnsTrue()
    {
        var provider = new Mock<ISessionMediaEditGraphProvider>();
        provider.Setup(p => p.HasEditGraph("play", "dev")).Returns(true);

        Assert.True(CreateHelper(graph: provider.Object).HasSessionEditGraphForRequest("play", "dev"));
        Assert.False(CreateHelper().HasSessionEditGraphForRequest("play", "dev"));
    }

    [Fact]
    public async Task ApplyPlaybackPlanTranscodeFlagsAsync_InvokesLoader()
    {
        var loader = new Mock<ISessionPlaybackPlanLoader>();
        loader
            .Setup(l => l.EnsurePlanLoadedAsync("play", "dev", "item", "source", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateHelper().ApplyPlaybackPlanTranscodeFlagsAsync(
            [loader.Object],
            BuildStreamingRequest(),
            "item",
            CancellationToken.None);

        loader.Verify(
            l => l.EnsurePlanLoadedAsync("play", "dev", "item", "source", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyPlaybackPlanTranscodeFlagsAsync_AudioFilter_DisablesAudioCopyOnly()
    {
        var audio = new Mock<ISessionAudioFilterProvider>();
        audio.Setup(p => p.GetAdditionalAudioFilter("play", "dev", 0)).Returns("volume=0");

        var request = BuildStreamingRequest();
        var result = await CreateHelper(audio: audio.Object).ApplyPlaybackPlanTranscodeFlagsAsync(
            Array.Empty<ISessionPlaybackPlanLoader>(),
            request,
            "item",
            CancellationToken.None);

        Assert.False(result.HasEditGraph);
        Assert.True(result.HasAudioFilter);
        Assert.False(request.AllowAudioStreamCopy);
        Assert.True(request.AllowVideoStreamCopy);
    }

    [Fact]
    public async Task ApplyPlaybackPlanTranscodeFlagsAsync_EditGraph_DisablesStreamCopy()
    {
        var graph = new Mock<ISessionMediaEditGraphProvider>();
        graph.Setup(p => p.HasEditGraph("play", "dev")).Returns(true);

        var request = BuildStreamingRequest();
        var result = await CreateHelper(graph: graph.Object).ApplyPlaybackPlanTranscodeFlagsAsync(
            null,
            request,
            "item",
            CancellationToken.None);

        Assert.True(result.HasEditGraph);
        Assert.False(result.HasAudioFilter);
        Assert.False(request.AllowAudioStreamCopy);
        Assert.False(request.AllowVideoStreamCopy);
    }

    private static StreamingRequestDto BuildStreamingRequest()
        => new()
        {
            PlaySessionId = "play",
            DeviceId = "dev",
            MediaSourceId = "source",
            AllowAudioStreamCopy = true,
            AllowVideoStreamCopy = true
        };

    private static Mock<ISessionMediaEditGraphProvider> CreateGraphProvider(SessionMediaEditGraph graph)
    {
        var provider = new Mock<ISessionMediaEditGraphProvider>();
        provider.Setup(p => p.HasEditGraph(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        provider
            .Setup(p => p.GetEditGraph(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SessionMediaEditGraphContext>()))
            .Returns(graph);
        return provider;
    }

    private static EncodingJobInfo BuildState()
    {
        var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac", Channels = 2 };

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mkv",
                MediaStreams = new List<MediaStream> { video, audio },
            },
            VideoStream = video,
            AudioStream = audio,
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
            RunTimeTicks = TimeSpan.FromMinutes(10).Ticks,
        };
    }

    private static EncodingHelper CreateHelper(
        ISessionAudioFilterProvider? audio = null,
        ISessionMediaEditGraphProvider? graph = null)
    {
        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>(),
            audio is null ? null : [audio],
            graph is null ? null : [graph]);
    }
}
