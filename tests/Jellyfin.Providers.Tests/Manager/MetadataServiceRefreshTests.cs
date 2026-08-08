using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public class MetadataServiceRefreshTests
    {
        [Theory]
        [InlineData(false, "existing overview")]
        [InlineData(true, null)]
        public async Task RefreshWithProviders_ReplaceAllMetadata_KeepsExistingDataOnProviderFailure(bool allProvidersSucceed, string? expectedOverview)
        {
            var item = new Movie
            {
                Name = "Test Movie",
                Overview = "existing overview"
            };

            // The provider owning the overview fails, so it contributes nothing to the replacement.
            var failing = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            failing.Setup(p => p.Name).Returns("Failing");
            failing.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .Returns(allProvidersSucceed
                    ? Task.FromResult(new MetadataResult<Movie> { HasMetadata = true, Item = new Movie() })
                    : Task.FromException<MetadataResult<Movie>>(new FormatException("bad id")));

            var succeeding = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            succeeding.Setup(p => p.Name).Returns("Succeeding");
            succeeding.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MetadataResult<Movie>
                {
                    HasMetadata = true,
                    Item = new Movie { Name = "Test Movie", Tagline = "new tagline" }
                });

            var service = new TestMetadataService();
            var result = await service.RefreshWithProvidersInternal(
                new MetadataResult<Movie> { Item = item },
                new MovieInfo { Name = item.Name },
                new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = true,
                    RemoveOldMetadata = true
                },
                [failing.Object, succeeding.Object]).ConfigureAwait(true);

            Assert.Equal(allProvidersSucceed ? 0 : 1, result.Failures);
            Assert.Equal("new tagline", item.Tagline);
            Assert.Equal(expectedOverview, item.Overview);
        }

        [Fact]
        public async Task RefreshWithProviders_ForeignProviderId_NotStored()
        {
            var item = new Movie { Name = "Test Movie" };

            var provider = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            provider.Setup(p => p.Name).Returns("Provider");
            provider.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var found = new Movie { Name = "Test Movie" };
                    found.ProviderIds[MetadataProvider.Tmdb.ToString()] = "nm0000123";
                    found.ProviderIds[MetadataProvider.Imdb.ToString()] = "tt0113375";
                    return new MetadataResult<Movie> { HasMetadata = true, Item = found };
                });

            var service = new TestMetadataService();
            await service.RefreshWithProvidersInternal(
                new MetadataResult<Movie> { Item = item },
                new MovieInfo { Name = item.Name },
                new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = true
                },
                [provider.Object]).ConfigureAwait(true);

            Assert.False(item.HasProviderId(MetadataProvider.Tmdb));
            Assert.Equal("tt0113375", item.GetProviderId(MetadataProvider.Imdb));
        }

        private sealed class TestMetadataService : MetadataService<Movie, MovieInfo>
        {
            public TestMetadataService()
                : base(
                    Mock.Of<IServerConfigurationManager>(),
                    NullLogger<MetadataService<Movie, MovieInfo>>.Instance,
                    Mock.Of<IProviderManager>(),
                    Mock.Of<IFileSystem>(),
                    Mock.Of<ILibraryManager>(),
                    Mock.Of<IExternalDataManager>(),
                    Mock.Of<IItemRepository>())
            {
            }

            public Task<RefreshResult> RefreshWithProvidersInternal(
                MetadataResult<Movie> metadata,
                MovieInfo id,
                MetadataRefreshOptions options,
                ICollection<IMetadataProvider> providers)
                => RefreshWithProviders(metadata, id, options, providers, ImageProvider, false, CancellationToken.None);
        }
    }
}
