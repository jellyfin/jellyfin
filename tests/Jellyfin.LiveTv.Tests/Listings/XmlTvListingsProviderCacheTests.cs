using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public sealed class XmlTvListingsProviderCacheTests : IDisposable
{
    private const string ChannelId = "3297";

    private readonly string _cachePath = Path.Combine(Path.GetTempPath(), "jellyfin-xmltv-tests-" + Guid.NewGuid().ToString("N"));

    private readonly ListingsProviderInfo _info = new()
    {
        Id = "cachetests",
        Path = "https://example.com/notitle.xml"
    };

    private bool _downloadsFail;
    private Exception? _downloadException;

    public void Dispose()
    {
        if (Directory.Exists(_cachePath))
        {
            Directory.Delete(_cachePath, true);
        }
    }

    [Fact]
    public async Task GetProgramsAsync_DownloadFailsAfterASuccess_KeepsUsingTheCachedListings()
    {
        var provider = CreateProvider();

        Assert.NotEmpty(await GetPrograms(provider));

        // Age the cached copy out, so the next call goes back to the (now broken) source.
        var cacheFile = Path.Combine(_cachePath, "xmltv", _info.Id + ".xml");
        File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddDays(-1));
        _downloadsFail = true;

        // Losing the listings entirely because a single download failed empties the whole guide.
        Assert.NotEmpty(await GetPrograms(provider));
        Assert.True(File.Exists(cacheFile));
    }

    [Fact]
    public async Task GetProgramsAsync_DownloadTimesOut_DoesNotSurfaceAsCancellation()
    {
        var provider = CreateProvider();

        // This is how HttpClient reports its own timeout. Left as an OperationCanceledException it
        // aborts the guide refresh for every channel and provider instead of only this one.
        _downloadsFail = true;
        _downloadException = new TaskCanceledException("timeout", new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() => GetPrograms(provider));
    }

    [Fact]
    public async Task GetProgramsAsync_ProviderSavedAfterAFailure_DownloadsAgain()
    {
        var provider = CreateProvider();

        _downloadsFail = true;
        await Assert.ThrowsAnyAsync<Exception>(() => GetPrograms(provider));

        // Without clearing the backoff the guide stays empty for an hour, even though saving the
        // provider deletes the cached file and is the user asking for another attempt.
        _downloadsFail = false;
        await provider.Validate(_info, true, true);

        Assert.NotEmpty(await GetPrograms(provider));
    }

    private async Task<ProgramInfo[]> GetPrograms(XmlTvListingsProvider provider)
    {
        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var programs = await provider.GetProgramsAsync(_info, ChannelId, startDate, startDate.AddDays(1), CancellationToken.None);

        return programs.ToArray();
    }

    private XmlTvListingsProvider CreateProvider()
    {
        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((m, _) =>
            {
                if (_downloadException is not null)
                {
                    return Task.FromException<HttpResponseMessage>(_downloadException);
                }

                if (_downloadsFail)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(File.OpenRead(Path.Combine("Test Data/LiveTv/Listings/XmlTv", m.RequestUri!.Segments[^1])))
                });
            });

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(messageHandler.Object));

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(x => x.CachePath).Returns(_cachePath);

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(x => x.ApplicationPaths).Returns(appPaths.Object);
        config.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());

        return new XmlTvListingsProvider(
            config.Object,
            httpClientFactory.Object,
            NullLogger<XmlTvListingsProvider>.Instance);
    }
}
