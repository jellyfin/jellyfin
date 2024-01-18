using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.LiveTv.TunerHosts.HdHomerun;
using MediaBrowser.Model.LiveTv;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class HdHomerunHostTests
    {
        private readonly Fixture _fixture;
        private readonly HdHomerunHost _hdHomerunHost;

        public HdHomerunHostTests()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (m, _) =>
                    {
                        return Task.FromResult(new HttpResponseMessage()
                        {
                            Content = new StreamContent(File.OpenRead(Path.Combine("Test Data/LiveTv", m.RequestUri!.Host, m.RequestUri.Segments[^1])))
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
            _hdHomerunHost = _fixture.Create<HdHomerunHost>();
        }

        [Fact]
        public async Task GetModelInfo_Valid_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = "192.168.1.182"
            };

            var modelInfo = await _hdHomerunHost.GetModelInfo(host, true, CancellationToken.None);
            Assert.Equal("HDHomeRun PRIME", modelInfo.FriendlyName);
            Assert.Equal("HDHR3-CC", modelInfo.ModelNumber);
            Assert.Equal("hdhomerun3_cablecard", modelInfo.FirmwareName);
            Assert.Equal("20160630atest2", modelInfo.FirmwareVersion);
            Assert.Equal("FFFFFFFF", modelInfo.DeviceID);
            Assert.Equal("FFFFFFFF", modelInfo.DeviceAuth);
            Assert.Equal(3, modelInfo.TunerCount);
            Assert.Equal("http://192.168.1.182:80", modelInfo.BaseURL);
            Assert.Equal("http://192.168.1.182:80/lineup.json", modelInfo.LineupURL);
        }

        [Fact]
        public async Task GetModelInfo_Legacy_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = "10.10.10.100"
            };

            var modelInfo = await _hdHomerunHost.GetModelInfo(host, true, CancellationToken.None);
            Assert.Equal("HDHomeRun DUAL", modelInfo.FriendlyName);
            Assert.Equal("HDHR3-US", modelInfo.ModelNumber);
            Assert.Equal("hdhomerun3_atsc", modelInfo.FirmwareName);
            Assert.Equal("20200225", modelInfo.FirmwareVersion);
            Assert.Equal("10xxxxx5", modelInfo.DeviceID);
            Assert.Null(modelInfo.DeviceAuth);
            Assert.Equal(2, modelInfo.TunerCount);
            Assert.Equal("http://10.10.10.100:80", modelInfo.BaseURL);
            Assert.Null(modelInfo.LineupURL);
        }

        [Fact]
        public async Task GetModelInfo_EmptyUrl_ArgumentException()
        {
            var host = new TunerHostInfo()
            {
                Url = string.Empty
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _hdHomerunHost.GetModelInfo(host, true, CancellationToken.None));
        }

        [Fact]
        public async Task GetLineup_Valid_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = "192.168.1.182"
            };

            var channels = await _hdHomerunHost.GetLineup(host, CancellationToken.None);
            Assert.Equal(6, channels.Count);
            Assert.Equal("4.1", channels[0].GuideNumber);
            Assert.Equal("WCMH-DT", channels[0].GuideName);
            Assert.True(channels[0].HD);
            Assert.True(channels[0].Favorite);
            Assert.Equal("http://192.168.1.111:5004/auto/v4.1", channels[0].URL);
        }

        [Fact]
        public async Task GetLineup_Legacy_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = "10.10.10.100"
            };

            // Placeholder json is invalid, just need to make sure we can reach it
            await Assert.ThrowsAsync<JsonException>(() => _hdHomerunHost.GetLineup(host, CancellationToken.None));
        }

        [Fact]
        public async Task GetLineup_ImportFavoritesOnly_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = "192.168.1.182",
                ImportFavoritesOnly = true
            };

            var channels = await _hdHomerunHost.GetLineup(host, CancellationToken.None);
            Assert.Single(channels);
            Assert.Equal("4.1", channels[0].GuideNumber);
            Assert.Equal("WCMH-DT", channels[0].GuideName);
            Assert.True(channels[0].HD);
            Assert.True(channels[0].Favorite);
            Assert.Equal("http://192.168.1.111:5004/auto/v4.1", channels[0].URL);
        }

        [Fact]
        public async Task TryGetTunerHostInfo_Valid_Success()
        {
            var host = await _hdHomerunHost.TryGetTunerHostInfo("192.168.1.182", CancellationToken.None);
            Assert.Equal(_hdHomerunHost.Type, host.Type);
            Assert.Equal("192.168.1.182", host.Url);
            Assert.Equal("HDHomeRun PRIME", host.FriendlyName);
            Assert.Equal("FFFFFFFF", host.DeviceId);
            Assert.Equal(3, host.TunerCount);
        }
    }
}
