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
            const string ResourceName = "Jellyfin.Server.Implementations.Tests.LiveTv.discover.json";

            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(
                    () => Task.FromResult(new HttpResponseMessage()
                    {
                        Content = new StreamContent(typeof(HdHomerunHostTests).Assembly.GetManifestResourceStream(ResourceName)!)
                    }));

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
    }
}
