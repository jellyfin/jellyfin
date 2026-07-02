using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class XmlTvListingsProviderTests
{
    private readonly Fixture _fixture;
    private readonly XmlTvListingsProvider _xmlTvListingsProvider;

    public XmlTvListingsProviderTests()
    {
        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(
                (m, _) =>
                {
                    return Task.FromResult(new HttpResponseMessage()
                    {
                        Content = new StreamContent(File.OpenRead(Path.Combine("Test Data/LiveTv/Listings/XmlTv", m.RequestUri!.Segments[^1])))
                    });
                });

        var http = new Mock<IHttpClientFactory>();
        http.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(messageHandler.Object));
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization
        {
            ConfigureMembers = true
        }).Inject(http);
        _xmlTvListingsProvider = _fixture.Create<XmlTvListingsProvider>();
    }

    [Theory]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/notitle.xml")]
    [InlineData("https://example.com/notitle.xml")]
    public async Task GetProgramsAsync_NoTitle_Success(string path)
    {
        var info = new ListingsProviderInfo()
        {
            Path = path
        };

        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var programs = await _xmlTvListingsProvider.GetProgramsAsync(info, "3297", startDate, startDate.AddDays(1), CancellationToken.None);
        var programsList = programs.ToList();
        Assert.Single(programsList);
        var program = programsList[0];
        Assert.Null(program.Name);
        Assert.Null(program.SeriesId);
        Assert.Null(program.EpisodeTitle);
        Assert.True(program.IsSports);
        Assert.True(program.HasImage);
        Assert.Equal("https://domain.tld/image.png", program.ImageUrl);
        Assert.Equal("3297", program.ChannelId);
        AssertXmlTvEtag(program.Etag);
    }

    [Theory]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/emptycategory.xml")]
    [InlineData("https://example.com/emptycategory.xml")]
    public async Task GetProgramsAsync_EmptyCategories_Success(string path)
    {
        var info = new ListingsProviderInfo()
        {
            Path = path
        };

        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var programs = await _xmlTvListingsProvider.GetProgramsAsync(info, "3297", startDate, startDate.AddDays(1), CancellationToken.None);
        var programsList = programs.ToList();
        Assert.Single(programsList);
        var program = programsList[0];
        Assert.DoesNotContain(program.Genres, g => string.IsNullOrEmpty(g));
        Assert.Equal("3297", program.ChannelId);
        AssertXmlTvEtag(program.Etag);
    }

    [Fact]
    public async Task GetProgramsAsync_Etag_SameContentIsStable()
    {
        var first = await GetSingleProgramAsync("Test Data/LiveTv/Listings/XmlTv/etag-base.xml");
        var second = await GetSingleProgramAsync("Test Data/LiveTv/Listings/XmlTv/etag-base.xml");

        Assert.Equal(first.Etag, second.Etag);
    }

    [Theory]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-title-change.xml")]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-description-change.xml")]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-icon-change.xml")]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-category-change.xml")]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-progid-change.xml")]
    public async Task GetProgramsAsync_Etag_ChangesWhenMappedContentChanges(string changedPath)
    {
        var original = await GetSingleProgramAsync("Test Data/LiveTv/Listings/XmlTv/etag-base.xml");
        var changed = await GetSingleProgramAsync(changedPath);

        Assert.NotEqual(original.Etag, changed.Etag);
    }

    [Theory]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-reordered.xml")]
    [InlineData("Test Data/LiveTv/Listings/XmlTv/etag-unknown-field.xml")]
    public async Task GetProgramsAsync_Etag_DoesNotChangeWhenMappedContentIsEquivalent(string equivalentPath)
    {
        var original = await GetSingleProgramAsync("Test Data/LiveTv/Listings/XmlTv/etag-base.xml");
        var equivalent = await GetSingleProgramAsync(equivalentPath);

        Assert.Equal(original.Etag, equivalent.Etag);
    }

    private async Task<ProgramInfo> GetSingleProgramAsync(string path)
    {
        var info = new ListingsProviderInfo()
        {
            Id = Path.GetFileNameWithoutExtension(path),
            Path = path
        };

        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var programs = await _xmlTvListingsProvider.GetProgramsAsync(info, "3297", startDate, startDate.AddDays(1), CancellationToken.None);

        return Assert.Single(programs.ToList());
    }

    private static void AssertXmlTvEtag(string? etag)
    {
        Assert.NotNull(etag);
        Assert.StartsWith("xmltv-sha256-v1:", etag!, StringComparison.Ordinal);
    }
}
