using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.Updates;
using MediaBrowser.Model.Updates;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Updates
{
    public class InstallationManagerTests
    {
        private readonly Fixture _fixture;
        private readonly InstallationManager _installationManager;

        public InstallationManagerTests()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (m, _) =>
                    {
                        return Task.FromResult(new HttpResponseMessage()
                        {
                            Content = new StreamContent(File.OpenRead("Test Data/Updates/" + m.RequestUri?.Segments[^1]))
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
            _installationManager = _fixture.Create<InstallationManager>();
        }

        [Fact]
        public async Task GetPackages_Valid_Success()
        {
            IList<PackageInfo> packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/releases/plugin/manifest-stable.json",
                false);

            Assert.Equal(25, packages.Count);
        }
    }
}
