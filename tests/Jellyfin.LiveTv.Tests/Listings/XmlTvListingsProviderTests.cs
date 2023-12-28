using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.LiveTv.Listings;
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

        var startDate = new DateTime(2022, 11, 4);
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

        var startDate = new DateTime(2022, 11, 4);
        var programs = await _xmlTvListingsProvider.GetProgramsAsync(info, "3297", startDate, startDate.AddDays(1), CancellationToken.None);
        var programsList = programs.ToList();
        Assert.Single(programsList);
        var program = programsList[0];
        Assert.DoesNotContain(program.Genres, g => string.IsNullOrEmpty(g));
        Assert.Equal("3297", program.ChannelId);
    }
}
