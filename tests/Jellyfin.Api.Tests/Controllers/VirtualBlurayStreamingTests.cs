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
using MediaBrowser.MediaEncoding.BdInfo;
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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public sealed class VirtualBlurayStreamingTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        Guid.NewGuid().ToString("N"));

    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Mock<IMediaEncoder> _encoder;
    private readonly IHost _host;
    private readonly MediaSourceInfo _source;
    private readonly string _clip;
    private readonly VirtualBlurayImageManager _images;
    private readonly ServerConfiguration _configuration = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public VirtualBlurayStreamingTests()
    {
        Directory.CreateDirectory(_directory);
        Directory.CreateDirectory(Path.Combine(_directory, "BDMV", "STREAM"));
        File.WriteAllBytes(Path.Combine(_directory, "BDMV", "index.bdmv"), [1]);
        _clip = Path.Combine(_directory, "BDMV", "STREAM", "00003.m2ts");
        File.WriteAllBytes(_clip, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Inject<IMemoryCache>(_cache);
        _encoder = fixture.Freeze<Mock<IMediaEncoder>>();
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration
            .Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _directory });
        configuration
            .SetupGet(c => c.CommonApplicationPaths)
            .Returns(Mock.Of<IApplicationPaths>(p => p.CachePath == _directory));
        configuration.SetupGet(c => c.Configuration).Returns(_configuration);
        _images = new VirtualBlurayImageManager(
            _cache,
            NullLogger<VirtualBlurayImageManager>.Instance,
            _ => false);
        fixture.Inject<IVirtualBlurayImageManager>(_images);
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
    }

    private string Url => $"/Videos/{_itemId}/stream.iso?static=true";

    [Theory]
    [InlineData("")]
    [InlineData(".iso")]
    public async Task StaticStream_MultipleClipsReportsImageLengthAndSupportsRanges(
        string extension)
    {
        var secondClip = Path.Combine(_directory, "BDMV", "STREAM", "00004.m2ts");
        File.WriteAllBytes(secondClip, [8, 9, 10, 11]);
        using var client = _host.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream{extension}?static=true");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_images.GetImage(_directory)!.Length, response.Content.Headers.ContentLength);
        Assert.Contains("bytes", response.Headers.AcceptRanges);
        _encoder.Verify(e => e.GetPrimaryPlaylistM2tsFiles(It.IsAny<string>()), Times.Never);

        using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        rangeRequest.Headers.Add("Range", "bytes=32768-34815");
        using var rangeResponse = await client.SendAsync(
            rangeRequest,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.PartialContent, rangeResponse.StatusCode);
        Assert.Equal(
            _images.GetImage(_directory)!.Length,
            rangeResponse.Content.Headers.ContentRange!.Length);
        var actual = await rangeResponse.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken);
        using var expected = _images.GetImage(_directory)!.Open();
        expected.Position = 32768;
        var bytes = new byte[2048];
        expected.ReadExactly(bytes);
        Assert.Equal(bytes, actual);
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".iso")]
    public async Task Head_ReportsImageLengthAndRangeSupportWithoutEncoding(string extension)
    {
        using var client = _host.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/Videos/{_itemId}/stream{extension}?static=true");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_images.GetImage(_directory)!.Length, response.Content.Headers.ContentLength);
        Assert.Contains("bytes", response.Headers.AcceptRanges);
        _encoder.Verify(e => e.GetPrimaryPlaylistM2tsFiles(It.IsAny<string>()), Times.Never);
        Assert.Empty(
            await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("bytes=32768-34815")]
    [InlineData("bytes=-4096")]
    [InlineData("bytes=610000-")]
    public async Task Range_MatchesVirtualImage(string range)
    {
        using var client = _host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Headers.Add("Range", range);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        var actual = await response.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken);
        using var expected = _images.GetImage(_directory)!.Open();
        expected.Position = response.Content.Headers.ContentRange!.From!.Value;
        var bytes = new byte[actual.Length];
        expected.ReadExactly(bytes);
        Assert.Equal(bytes, actual);
    }

    [Fact]
    public async Task InvalidRange_Returns416WithImageLength()
    {
        using var client = _host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Headers.Add("Range", "bytes=999999999999-");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
        Assert.Equal(
            _images.GetImage(_directory)!.Length,
            response.Content.Headers.ContentRange!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".iso")]
    public async Task DvdFolder_StaticStreamSupportsHeadAndRangeWithoutEncoding(string extension)
    {
        Directory.Delete(Path.Combine(_directory, "BDMV"), true);
        var videoTs = Directory.CreateDirectory(Path.Combine(_directory, "VIDEO_TS"));
        File.WriteAllBytes(Path.Combine(videoTs.FullName, "VIDEO_TS.IFO"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(videoTs.FullName, "VTS_01_1.VOB"), [4, 5, 6]);
        _source.VideoType = VideoType.Dvd;
        _source.Container = "dvd";
        using var client = _host.GetTestClient();
        var url = $"/Videos/{_itemId}/stream{extension}?static=true";
        using var head = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await client.SendAsync(head, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_images.GetImage(_directory)!.Length, response.Content.Headers.ContentLength);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Range", "bytes=32768-34815");
        using var range = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.PartialContent, range.StatusCode);
        using var expected = _images.GetImage(_directory)!.Open();
        expected.Position = 32768;
        var bytes = new byte[2048];
        expected.ReadExactly(bytes);
        Assert.Equal(
            bytes,
            await range.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        _encoder.Verify(
            e => e.GetPrimaryPlaylistVobFiles(It.IsAny<string>(), It.IsAny<uint?>()),
            Times.Never);
        _encoder.Verify(
            e => e.GenerateConcatConfig(It.IsAny<MediaSourceInfo>(), It.IsAny<string>()),
            Times.Never);
        _encoder.Verify(
            e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(VideoType.Dvd, _source.VideoType);
        Assert.Equal(_directory, _source.Path);
    }

    public void Dispose()
    {
        _host.Dispose();
        _images.Dispose();
        _cache.Dispose();
        Directory.Delete(_directory, true);
    }
}
