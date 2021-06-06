using System;
using System.IO;
using System.Linq;
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
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/releases/plugin/manifest-stable.json",
                false);

            Assert.Equal(25, packages.Length);
        }

        [Fact]
        public async Task FilterPackages_NameOnly_Success()
        {
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/releases/plugin/manifest-stable.json",
                false);

            packages = _installationManager.FilterPackages(packages, "Anime").ToArray();
            Assert.Single(packages);
        }

        [Fact]
        public async Task FilterPackages_GuidOnly_Success()
        {
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/releases/plugin/manifest-stable.json",
                false);

            packages = _installationManager.FilterPackages(packages, id: new Guid("a4df60c5-6ab4-412a-8f79-2cab93fb2bc5")).ToArray();
            Assert.Single(packages);
        }
    }
}
