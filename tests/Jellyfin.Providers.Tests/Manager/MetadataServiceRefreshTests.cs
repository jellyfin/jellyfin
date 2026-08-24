using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
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
        // RemoveOldMetadata is only ever set by an explicit user action - a refresh with "replace all
        // metadata", or Identify. A provider failing must not silently downgrade that to a merge: the
        // providers that did answer supplied the replacement, and the old values are the wrong match
        // the user asked to get rid of.
        [InlineData(false)]
        [InlineData(true)]
        public async Task RefreshWithProviders_ReplaceAllMetadata_ErasesOldDataWhenAProviderAnswers(bool allProvidersSucceed)
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
            Assert.Null(item.Overview);
        }

        [Fact]
        public async Task RefreshWithProviders_ReplaceAllMetadata_KeepsExistingDataWhenEveryRemoteProviderFails()
        {
            var item = new Movie
            {
                Name = "Test Movie",
                Overview = "existing overview"
            };

            // Something has to contribute for the merge to run at all, otherwise the item is never touched
            // and the case is moot. The local provider is the replacement the remote ones did not deliver.
            var local = new Mock<ILocalMetadataProvider<Movie>>(MockBehavior.Loose);
            local.Setup(p => p.Name).Returns("Local");
            local.Setup(p => p.GetMetadata(It.IsAny<ItemInfo>(), It.IsAny<IDirectoryService>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MetadataResult<Movie>
                {
                    HasMetadata = true,
                    Item = new Movie { Name = "Test Movie", Tagline = "new tagline" }
                });

            var remote = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            remote.Setup(p => p.Name).Returns("Failing");
            remote.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<MetadataResult<Movie>>(new HttpRequestException("unreachable")));

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
                [local.Object, remote.Object]).ConfigureAwait(true);

            Assert.Equal(1, result.Failures);
            Assert.Equal("new tagline", item.Tagline);

            // No remote provider answered, so erasing the overview would lose it for good.
            Assert.Equal("existing overview", item.Overview);
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

        [Fact]
        public async Task RefreshWithProviders_ForeignProviderId_ReplacedInLookupInfo()
        {
            var item = new Movie { Name = "Test Movie" };
            var lookupInfo = new MovieInfo { Name = item.Name };
            lookupInfo.ProviderIds[MetadataProvider.Tmdb.ToString()] = "nm0000123";

            var answering = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            answering.Setup(p => p.Name).Returns("Answering");
            answering.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var found = new Movie { Name = "Test Movie" };
                    found.ProviderIds[MetadataProvider.Tmdb.ToString()] = "12345";
                    return new MetadataResult<Movie> { HasMetadata = true, Item = found };
                });

            string? tmdbIdSeenBySecondProvider = null;
            var following = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            following.Setup(p => p.Name).Returns("Following");
            following.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MovieInfo info, CancellationToken _) =>
                {
                    tmdbIdSeenBySecondProvider = info.GetProviderId(MetadataProvider.Tmdb);
                    return new MetadataResult<Movie> { HasMetadata = false };
                });

            var service = new TestMetadataService();
            await service.RefreshWithProvidersInternal(
                new MetadataResult<Movie> { Item = item },
                lookupInfo,
                new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = true
                },
                [answering.Object, following.Object]).ConfigureAwait(true);

            // The stored id cannot be a TMDb one, so the provider that still has to run must get the id
            // that was just found instead of failing on the same bad one.
            Assert.Equal("12345", tmdbIdSeenBySecondProvider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RefreshWithProviders_ForeignPersonProviderId_NotStored(bool replaceAllMetadata)
        {
            var item = new Movie { Name = "Test Movie" };
            var existing = new MetadataResult<Movie> { Item = item };
            existing.AddPerson(new PersonInfo { Name = "Some Actor", Type = PersonKind.Actor });

            var provider = new Mock<IRemoteMetadataProvider<Movie, MovieInfo>>(MockBehavior.Loose);
            provider.Setup(p => p.Name).Returns("Provider");
            provider.Setup(p => p.GetMetadata(It.IsAny<MovieInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var person = new PersonInfo { Name = "Some Actor", Type = PersonKind.Actor };
                    person.ProviderIds[MetadataProvider.Tmdb.ToString()] = "nm0000123";
                    person.ProviderIds[MetadataProvider.Imdb.ToString()] = "nm0000123";

                    var found = new MetadataResult<Movie> { HasMetadata = true, Item = new Movie { Name = "Test Movie" } };
                    found.AddPerson(person);
                    return found;
                });

            var service = new TestMetadataService();
            await service.RefreshWithProvidersInternal(
                existing,
                new MovieInfo { Name = item.Name },
                new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = replaceAllMetadata
                },
                [provider.Object]).ConfigureAwait(true);

            var mergedPerson = Assert.Single(existing.People);
            Assert.False(mergedPerson.HasProviderId(MetadataProvider.Tmdb));
            Assert.Equal("nm0000123", mergedPerson.GetProviderId(MetadataProvider.Imdb));
        }

        [Theory]
        [InlineData(MetadataRefreshMode.FullRefresh, true)]
        [InlineData(MetadataRefreshMode.Default, false)]
        public async Task RefreshMetadata_ProvidersFoundNothing_PersistsRefreshDateOnFullRefresh(MetadataRefreshMode mode, bool expectSaved)
        {
            var item = new TestItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                PreferredMetadataLanguage = "en",
                PreferredMetadataCountryCode = "US",
                DateLastRefreshed = DateTime.UtcNow.AddDays(-60),
                DateLastSaved = DateTime.UtcNow.AddDays(-60)
            };
            item.PresentationUniqueKey = item.CreatePresentationUniqueKey();

            var stampBefore = item.DateLastRefreshed;

            var provider = new Mock<IRemoteMetadataProvider<TestItem, ItemLookupInfo>>(MockBehavior.Loose);
            provider.Setup(p => p.Name).Returns("Provider");
            provider.Setup(p => p.GetMetadata(It.IsAny<ItemLookupInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MetadataResult<TestItem> { HasMetadata = false });

            var libraryManager = new Mock<ILibraryManager>(MockBehavior.Loose);
            libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());

            var providerManager = new Mock<IProviderManager>(MockBehavior.Loose);
            providerManager.Setup(p => p.GetImageProviders(It.IsAny<BaseItem>(), It.IsAny<ImageRefreshOptions>()))
                .Returns(Array.Empty<IImageProvider>());
            providerManager.Setup(p => p.GetMetadataProviders<TestItem>(It.IsAny<BaseItem>(), It.IsAny<LibraryOptions>()))
                .Returns(new[] { (IMetadataProvider<TestItem>)provider.Object });
            providerManager.Setup(p => p.GetMetadataSavers(It.IsAny<BaseItem>(), It.IsAny<LibraryOptions>()))
                .Returns(Array.Empty<IMetadataSaver>());

            var itemRepository = new Mock<IItemRepository>(MockBehavior.Loose);
            itemRepository.Setup(r => r.ItemExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            var service = new TestItemMetadataService(libraryManager.Object, providerManager.Object, itemRepository.Object);

            await service.RefreshMetadata(
                item,
                new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
                {
                    MetadataRefreshMode = mode,
                    ImageRefreshMode = mode
                },
                CancellationToken.None).ConfigureAwait(true);

            // Nothing was found, so on a full refresh the advanced stamp is the only reason to write the row.
            Assert.Equal(expectSaved, item.Saved);

            if (expectSaved)
            {
                Assert.True(item.DateLastRefreshed > stampBefore);
            }
        }

        /// <summary>
        /// Stands in for a real item so the refresh stays off the shared BaseItem statics, which other
        /// test classes in this assembly overwrite while xUnit runs them in parallel.
        /// </summary>
        internal sealed class TestItem : BaseItem
        {
            public bool Saved { get; private set; }

            public override bool RequiresRefresh() => false;

            public override bool IsSaveLocalMetadataEnabled() => false;

            public override string CreatePresentationUniqueKey() => Id.ToString("N", CultureInfo.InvariantCulture);

            public override ItemUpdateType OnMetadataChanged() => ItemUpdateType.None;

            public override bool BeforeMetadataRefresh(bool replaceAllMetadata) => false;

            public override Task UpdateToRepositoryAsync(ItemUpdateType updateReason, CancellationToken cancellationToken)
            {
                Saved = true;
                return Task.CompletedTask;
            }
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

        private sealed class TestItemMetadataService : MetadataService<TestItem, ItemLookupInfo>
        {
            public TestItemMetadataService(ILibraryManager libraryManager, IProviderManager providerManager, IItemRepository itemRepository)
                : base(
                    Mock.Of<IServerConfigurationManager>(),
                    NullLogger<MetadataService<TestItem, ItemLookupInfo>>.Instance,
                    providerManager,
                    Mock.Of<IFileSystem>(),
                    libraryManager,
                    Mock.Of<IExternalDataManager>(),
                    itemRepository)
            {
            }
        }
    }
}
