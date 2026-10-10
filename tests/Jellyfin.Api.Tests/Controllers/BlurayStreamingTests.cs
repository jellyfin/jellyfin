using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public sealed class BlurayStreamingTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        Guid.NewGuid().ToString("N"));

    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Mock<IMediaEncoder> _encoder;
    private readonly TestServer _server;
    private readonly IHost _host;
    private readonly MediaSourceInfo _source;
    private readonly string _clip;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public BlurayStreamingTests()
    {
        Directory.CreateDirectory(_directory);
        _clip = Path.Combine(_directory, "00003.m2ts");
        File.WriteAllBytes(_clip, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Inject<IMemoryCache>(_cache);
        _encoder = fixture.Freeze<Mock<IMediaEncoder>>();
        _encoder.Setup(e => e.GetPrimaryPlaylistM2tsFiles(_directory)).Returns(new[] { _clip });
        _encoder
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaInfo { RunTimeTicks = TimeSpan.FromSeconds(60).Ticks });
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration
            .Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _directory });
        configuration
            .SetupGet(c => c.CommonApplicationPaths)
            .Returns(Mock.Of<IApplicationPaths>(p => p.CachePath == _directory));
        _source = new MediaSourceInfo
        {
            Id = _itemId.ToString("N"),
            Path = _directory,
            Protocol = MediaProtocol.File,
            VideoType = VideoType.BluRay,
            Container = "mpegts",
            RunTimeTicks = TimeSpan.FromSeconds(60).Ticks,
            MediaStreams = new List<MediaStream>
            {
                new()
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                    Index = 0,
                    Width = 1920,
                    Height = 1080,
                    BitRate = 1000000,
                    AverageFrameRate = 24,
                },
                new()
                {
                    Type = MediaStreamType.Audio,
                    Codec = "ac3",
                    Index = 1,
                    Channels = 2,
                    BitRate = 192000,
                    SampleRate = 48000,
                },
            },
        };
        var movie = new Movie { Id = _itemId, Path = _directory };
        fixture
            .Freeze<Mock<ILibraryManager>>()
            .Setup(l => l.GetItemById<BaseItem>(_itemId))
            .Returns(movie);
        fixture
            .Freeze<Mock<IMediaSourceManager>>()
            .Setup(m =>
                m.GetPlaybackMediaSources(movie, null, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _source });
        var controller = fixture.Build<VideosController>().OmitAutoProperties().Create();
        _host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddAuthentication();
                        services.AddAuthorization();
                        services
                            .AddControllers()
                            .AddApplicationPart(typeof(VideosController).Assembly)
                            .AddControllersAsServices();
                        services.AddSingleton(controller);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers().AllowAnonymous());
                    }))
            .Start();
        _server = _host.GetTestServer();
    }

    [Fact]
    public async Task Head_ThenRange_ReusesClipProbe()
    {
        using var client = _server.CreateClient();
        using var head = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream?static=true");
        using var headResponse = await client.SendAsync(
            head,
            TestContext.Current.CancellationToken);
        using var range = new HttpRequestMessage(
            HttpMethod.Get,
            $"/Videos/{_itemId}/stream?static=true");
        range.Headers.Add("Range", "bytes=2-4");
        using var response = await client.SendAsync(range, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(
            new byte[] { 2, 3, 4 },
            await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentRequests_ProbeOnce()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<MediaInfo>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _encoder
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                entered.TrySetResult();
                return release.Task;
            });
        var first = ResolveClip();
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = ResolveClip();
        try
        {
            Assert.False(second.IsCompleted);
        }
        finally
        {
            release.TrySetResult(new MediaInfo { RunTimeTicks = _source.RunTimeTicks });
        }

        Assert.IsType<PhysicalFileResult>(await first);
        Assert.IsType<PhysicalFileResult>(await second);
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangedClip_Reprobes(bool changeLength)
    {
        Assert.IsType<PhysicalFileResult>(await ResolveClip());
        if (changeLength)
        {
            await File.WriteAllBytesAsync(
                _clip,
                new byte[9],
                TestContext.Current.CancellationToken);
        }
        else
        {
            File.SetLastWriteTimeUtc(_clip, File.GetLastWriteTimeUtc(_clip).AddMinutes(1));
        }

        _encoder
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaInfo { RunTimeTicks = TimeSpan.FromSeconds(30).Ticks });
        Assert.Null(await ResolveClip());
        Assert.Equal(1, _cache.Count);
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CachedClip_StillChecksPlaylistAndTitleDuration()
    {
        Assert.IsType<PhysicalFileResult>(await ResolveClip());
        _source.RunTimeTicks = TimeSpan.FromSeconds(30).Ticks;
        Assert.Null(await ResolveClip());
        _source.RunTimeTicks = TimeSpan.FromSeconds(60).Ticks;
        _encoder
            .Setup(e => e.GetPrimaryPlaylistM2tsFiles(_directory))
            .Returns(new[] { _clip, _clip });
        Assert.Null(await ResolveClip());
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedProbe_IsRetried(bool cancelled)
    {
        _encoder
            .SetupSequence(e =>
                e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                cancelled ? new OperationCanceledException() : new IOException("Probe failed"))
            .ReturnsAsync(new MediaInfo { RunTimeTicks = _source.RunTimeTicks });
        await Assert.ThrowsAnyAsync<Exception>(() => ResolveClip());
        Assert.IsType<PhysicalFileResult>(await ResolveClip());
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private Task<ActionResult?> ResolveClip() =>
        FileStreamResponseHelpers.GetStaticBlurayFileResult(
            _source,
            _encoder.Object,
            _cache,
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Head_SingleClip_ReturnsActualLengthAndRangeSupport()
    {
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream?static=true");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, response.Content.Headers.ContentLength);
        Assert.Contains("bytes", response.Headers.AcceptRanges);
        Assert.Empty(
            await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.OK, 8, 0, 7)]
    [InlineData("bytes=2-4", HttpStatusCode.PartialContent, 3, 2, 4)]
    [InlineData("bytes=5-", HttpStatusCode.PartialContent, 3, 5, 7)]
    [InlineData("bytes=-2", HttpStatusCode.PartialContent, 2, 6, 7)]
    public async Task Get_SingleClip_ReturnsOriginalBytes(
        string? range,
        HttpStatusCode status,
        int length,
        byte first,
        byte last)
    {
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/Videos/{_itemId}/stream?static=true");
        if (range is not null)
        {
            request.Headers.Add("Range", range);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(length, response.Content.Headers.ContentLength);
        Assert.Equal("video/mp2t", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(length, bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            Assert.Equal(first + i, bytes[i]);
        }

        Assert.Equal(last, bytes[^1]);
        if (range is not null)
        {
            Assert.Equal(
                $"bytes {first}-{last}/8",
                response.Content.Headers.ContentRange?.ToString());
        }

        _encoder.Verify(
            e => e.GenerateConcatConfig(It.IsAny<MediaSourceInfo>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Get_RangeBeyondEnd_Returns416()
    {
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/Videos/{_itemId}/stream?static=true");
        request.Headers.Add("Range", "bytes=8-");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
        Assert.Equal("bytes */8", response.Content.Headers.ContentRange?.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Head_NotSingleClip_KeepsProgressiveResponse(int count)
    {
        // Repeated references to the same clip are still a multi-entry playlist.
        _encoder
            .Setup(e => e.GetPrimaryPlaylistM2tsFiles(_directory))
            .Returns(count == 0 ? Array.Empty<string>() : new[] { _clip, _clip });
        await AssertProgressiveHead();
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(61)]
    public async Task Head_UnknownOrDifferentClipDuration_KeepsProgressiveResponse(int? seconds)
    {
        _encoder
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new MediaInfo
                {
                    RunTimeTicks = seconds.HasValue
                        ? TimeSpan.FromSeconds(seconds.Value).Ticks
                        : null,
                });
        await AssertProgressiveHead();
    }

    [Fact]
    public async Task Head_UnknownTitleDuration_KeepsProgressiveResponse()
    {
        _source.RunTimeTicks = null;
        await AssertProgressiveHead();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public async Task Head_UnknownFrameRate_KeepsProgressiveResponse(float? frameRate)
    {
        _source.MediaStreams[0].AverageFrameRate = frameRate;
        await AssertProgressiveHead();
    }

    [Theory]
    [InlineData(40, 8)]
    [InlineData(42, null)]
    public async Task Head_DurationRounding_AllowsAtMostOneFrame(
        int differenceMilliseconds,
        int? expectedLength)
    {
        _encoder
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new MediaInfo
                {
                    RunTimeTicks =
                        _source.RunTimeTicks
                        + TimeSpan.FromMilliseconds(differenceMilliseconds).Ticks,
                });
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream?static=true");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedLength, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task Head_OrdinaryVideo_KeepsStaticResponse()
    {
        _source.VideoType = VideoType.VideoFile;
        _source.Path = _clip;
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream?static=true");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, response.Content.Headers.ContentLength);
        _encoder.Verify(e => e.GetPrimaryPlaylistM2tsFiles(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Head_NonStaticRequest_KeepsProgressiveResponse()
    {
        await AssertProgressiveHead("false");
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private async Task AssertProgressiveHead(string staticValue = "true")
    {
        using var client = _server.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream?static={staticValue}");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentLength);
        Assert.Contains("none", response.Headers.AcceptRanges);
    }

    public void Dispose()
    {
        _server.Dispose();
        _host.Dispose();
        _cache.Dispose();
        Directory.Delete(_directory, true);
    }
}
