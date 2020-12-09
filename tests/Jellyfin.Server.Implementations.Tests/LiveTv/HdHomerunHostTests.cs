using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun;
using MediaBrowser.Model.LiveTv;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.LiveTv
{
    public class HdHomerunHostTests
    {
        private const string TestIp = "http://192.168.1.182";

        private readonly Fixture _fixture;
        private readonly HdHomerunHost _hdHomerunHost;

        public HdHomerunHostTests()
        {
            const string BaseResourcePath = "Jellyfin.Server.Implementations.Tests.LiveTv.";

            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (m, _) =>
                    {
                        var resource = BaseResourcePath + m.RequestUri?.Segments[^1];
                        var stream = typeof(HdHomerunHostTests).Assembly.GetManifestResourceStream(resource);
                        if (stream == null)
                        {
                            throw new NullReferenceException("Resource doesn't exist: " + resource);
                        }

                        return Task.FromResult(new HttpResponseMessage()
                        {
                            Content = new StreamContent(stream)
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
                Url = TestIp
            };

            var modelInfo = await _hdHomerunHost.GetModelInfo(host, true, CancellationToken.None).ConfigureAwait(false);
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
                Url = TestIp
            };

            var channels = await _hdHomerunHost.GetLineup(host, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(6, channels.Count);
            Assert.Equal("4.1", channels[0].GuideNumber);
            Assert.Equal("WCMH-DT", channels[0].GuideName);
            Assert.True(channels[0].HD);
            Assert.True(channels[0].Favorite);
            Assert.Equal("http://192.168.1.111:5004/auto/v4.1", channels[0].URL);
        }

        [Fact]
        public async Task GetLineup_ImportFavoritesOnly_Success()
        {
            var host = new TunerHostInfo()
            {
                Url = TestIp,
                ImportFavoritesOnly = true
            };

            var channels = await _hdHomerunHost.GetLineup(host, CancellationToken.None).ConfigureAwait(false);
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
            var host = await _hdHomerunHost.TryGetTunerHostInfo(TestIp, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(_hdHomerunHost.Type, host.Type);
            Assert.Equal(TestIp, host.Url);
            Assert.Equal("HDHomeRun PRIME", host.FriendlyName);
            Assert.Equal("FFFFFFFF", host.DeviceId);
            Assert.Equal(3, host.TunerCount);
        }
    }
}
