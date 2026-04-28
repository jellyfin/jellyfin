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
            });
            _fixture.Inject(http);
            _installationManager = _fixture.Create<InstallationManager>();
        }

        [Fact]
        public async Task GetPackages_Valid_Success()
        {
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/files/plugin/manifest.json",
                false,
                TestContext.Current.CancellationToken);

            Assert.Equal(25, packages.Length);
        }

        [Fact]
        public async Task FilterPackages_NameOnly_Success()
        {
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/files/plugin/manifest.json",
                false,
                TestContext.Current.CancellationToken);

            packages = _installationManager.FilterPackages(packages, "Anime").ToArray();
            Assert.Single(packages);
        }

        [Fact]
        public async Task FilterPackages_GuidOnly_Success()
        {
            PackageInfo[] packages = await _installationManager.GetPackages(
                "Jellyfin Stable",
                "https://repo.jellyfin.org/files/plugin/manifest.json",
                false,
                TestContext.Current.CancellationToken);

            packages = _installationManager.FilterPackages(packages, id: new Guid("a4df60c5-6ab4-412a-8f79-2cab93fb2bc5")).ToArray();
            Assert.Single(packages);
        }

        [Fact]
        public async Task InstallPackage_InvalidChecksum_ThrowsInvalidDataException()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "InvalidChecksum"
            };

            await Assert.ThrowsAsync<InvalidDataException>(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
        }

        [Fact]
        public async Task InstallPackage_UnknownChecksumAlgorithm_ThrowsInvalidDataException()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "sha999:3953e8eec12765950f32ec81cd590ac62825f84a355ee74d542516201519f8ae"
            };

            await Assert.ThrowsAsync<InvalidDataException>(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
        }

        [Fact]
        public async Task InstallPackage_InvalidChecksumDigestFormat_ThrowsInvalidDataException()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "sha256"
            };

            await Assert.ThrowsAsync<InvalidDataException>(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
        }

        [Fact]
        public async Task InstallPackage_Valid_Success()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "11b5b2f1a9ebc4f66d6ef19018543361"
            };

            var ex = await Record.ExceptionAsync(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
            Assert.Null(ex);
        }

        [Fact]
        public async Task InstallPackage_ValidSha256Digest_Success()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "sha256:3953e8eec12765950f32ec81cd590ac62825f84a355ee74d542516201519f8ae"
            };

            var ex = await Record.ExceptionAsync(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
            Assert.Null(ex);
        }

        [Fact]
        public async Task InstallPackage_ValidSha512Digest_Success()
        {
            var packageInfo = new InstallationInfo()
            {
                Name = "Test",
                SourceUrl = "https://repo.jellyfin.org/releases/plugin/empty/empty.zip",
                Checksum = "sha512:a6c651fcbbc74b95dc52647a312c332d9ecf6290821de099fee38797fa1c8057d48f3e99c935601f122d9afac65605515fa014944060177cf9ff18309a28a2d2"
            };

            var ex = await Record.ExceptionAsync(() => _installationManager.InstallPackage(packageInfo, CancellationToken.None));
            Assert.Null(ex);
        }
    }
}
