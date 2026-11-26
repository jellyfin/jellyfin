using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Providers.TV;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.TV;

// put tests that mock the static LibraryManager in the same collection to avoid test interference
[Collection("LibraryManagerTests")]
public sealed class EpisodeMetadataServiceTests : IDisposable
{
    private readonly TestEpisodeMetadataService _service = new();
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public EpisodeMetadataServiceTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;

        // A refresh asks the item itself whether to save local metadata, which reads the library
        // options straight off the static manager.
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Loose);
        libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());
        BaseItem.LibraryManager = libraryManager.Object;

        // Refreshing an episode reads Video.SourceType, which asks whether the file is an active
        // recording before anything else can run.
        _previousRecordingsManager = Video.RecordingsManager;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        Video.RecordingsManager = _previousRecordingsManager;
    }

    [Fact]
    public void MergeData_ProviderSeasonOverridesPathDerivedSeason()
    {
        var source = new MetadataResult<Episode>
        {
            Item = new Episode
            {
                ParentIndexNumber = 2
            }
        };

        var target = new MetadataResult<Episode>
        {
            Item = new Episode
            {
                ParentIndexNumber = 1
            }
        };

        _service.Merge(source, target, replaceData: false, mergeMetadataSettings: true);

        Assert.Equal(2, target.Item.ParentIndexNumber);
    }

    [Fact]
    public void MergeData_BackfillExistingMetadata_DoesNotOverrideProviderSeason()
    {
        var existingMetadata = new MetadataResult<Episode>
        {
            Item = new Episode
            {
                ParentIndexNumber = 1
            }
        };

        var temp = new MetadataResult<Episode>
        {
            Item = new Episode
            {
                ParentIndexNumber = 2
            }
        };

        _service.Merge(existingMetadata, temp, replaceData: false, mergeMetadataSettings: false);

        Assert.Equal(2, temp.Item.ParentIndexNumber);
    }

    [Fact]
    public void MergeData_MissingProviderSeasonKeepsExistingSeason()
    {
        var source = new MetadataResult<Episode>
        {
            Item = new Episode()
        };

        var target = new MetadataResult<Episode>
        {
            Item = new Episode
            {
                ParentIndexNumber = 1
            }
        };

        _service.Merge(source, target, replaceData: false, mergeMetadataSettings: true);

        Assert.Equal(1, target.Item.ParentIndexNumber);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(22, 21)]
    [InlineData(21, 2)] // e.g. "Series - S03E21 - E2 (1080p BluRay x265).mkv", where "E2" is the episode title
    public void BeforeSave_ReversedEpisodeRange_ClearsIndexNumberEnd(int indexNumber, int indexNumberEnd)
    {
        var item = new Episode
        {
            IndexNumber = indexNumber,
            IndexNumberEnd = indexNumberEnd
        };

        var updateType = _service.BeforeSave(item);

        // The episode number identifies the item, so it is kept and the impossible range is dropped
        Assert.Equal(indexNumber, item.IndexNumber);
        Assert.Null(item.IndexNumberEnd);
        Assert.True(updateType.HasFlag(ItemUpdateType.MetadataImport));
    }

    [Fact]
    public void BeforeSave_EpisodeRangeWithoutStart_ClearsIndexNumberEnd()
    {
        var item = new Episode
        {
            IndexNumber = null,
            IndexNumberEnd = 2
        };

        var updateType = _service.BeforeSave(item);

        Assert.Null(item.IndexNumberEnd);
        Assert.Null(item.IndexNumber);
        Assert.True(updateType.HasFlag(ItemUpdateType.MetadataImport));
    }

    [Theory]
    [InlineData(1, 2)] // Regular multi episode file
    [InlineData(1, 1)] // Degenerate but not contradictory
    public void BeforeSave_ValidEpisodeRange_KeepsIndexNumberEnd(int indexNumber, int indexNumberEnd)
    {
        var item = new Episode
        {
            IndexNumber = indexNumber,
            IndexNumberEnd = indexNumberEnd
        };

        _service.BeforeSave(item);

        Assert.Equal(indexNumber, item.IndexNumber);
        Assert.Equal(indexNumberEnd, item.IndexNumberEnd);
    }

    [Theory]
    // An episode id is positional - a provider derives it from the series id plus the episode number
    // under the series' display order - so a refresh that is meant to re-derive it has to stop handing
    // the old one back. Providers prefer a supplied id over the numbers, so leaving it in place makes
    // them return the metadata the stale id points at and the refresh looks like it did nothing.
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshMetadata_ReplaceAllMetadata_DropsStaleEpisodeIdFromLookup(bool replaceAllMetadata)
    {
        EpisodeInfo? lookup = null;

        var provider = new Mock<IRemoteMetadataProvider<Episode, EpisodeInfo>>(MockBehavior.Loose);
        provider.Setup(p => p.Name).Returns("Test Provider");
        provider.Setup(p => p.GetMetadata(It.IsAny<EpisodeInfo>(), It.IsAny<CancellationToken>()))
            .Callback<EpisodeInfo, CancellationToken>((info, _) => lookup = info)
            .ReturnsAsync(new MetadataResult<Episode> { HasMetadata = true, Item = new Episode() });

        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Name = "Test Episode",
            IndexNumber = 1,
            ParentIndexNumber = 1,
            PreferredMetadataLanguage = "en",
            PreferredMetadataCountryCode = "US"
        };

        // The id the previous order resolved to, pointing at a different episode than number 1.
        episode.ProviderIds[MetadataProvider.Tvdb.ToString()] = "219120";

        var providerManager = new Mock<IProviderManager>(MockBehavior.Loose);
        providerManager.Setup(p => p.GetMetadataProviders<Episode>(It.IsAny<BaseItem>(), It.IsAny<LibraryOptions>()))
            .Returns([provider.Object]);
        providerManager.Setup(p => p.GetImageProviders(It.IsAny<BaseItem>(), It.IsAny<ImageRefreshOptions>()))
            .Returns(Array.Empty<IImageProvider>());
        providerManager.Setup(p => p.GetMetadataSavers(It.IsAny<BaseItem>(), It.IsAny<LibraryOptions>()))
            .Returns(Array.Empty<IMetadataSaver>());

        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Loose);
        libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());

        var service = new TestEpisodeMetadataService(providerManager.Object, libraryManager.Object);

        await service.RefreshMetadata(
            episode,
            new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
            {
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ReplaceAllMetadata = replaceAllMetadata
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(lookup);

        if (replaceAllMetadata)
        {
            Assert.False(lookup!.ProviderIds.ContainsKey(MetadataProvider.Tvdb.ToString()));
        }
        else
        {
            // An ordinary refresh still gets the id, which is what keeps it a cheap direct fetch.
            Assert.Equal("219120", lookup!.ProviderIds[MetadataProvider.Tvdb.ToString()]);
        }
    }

    private sealed class TestEpisodeMetadataService : EpisodeMetadataService
    {
        public TestEpisodeMetadataService()
            : this(Mock.Of<IProviderManager>(), Mock.Of<ILibraryManager>())
        {
        }

        public TestEpisodeMetadataService(IProviderManager providerManager, ILibraryManager libraryManager)
            : base(
                Mock.Of<IServerConfigurationManager>(),
                NullLogger<EpisodeMetadataService>.Instance,
                providerManager,
                Mock.Of<IFileSystem>(),
                libraryManager,
                Mock.Of<IExternalDataManager>(),
                Mock.Of<IItemRepository>())
        {
        }

        public void Merge(MetadataResult<Episode> source, MetadataResult<Episode> target, bool replaceData, bool mergeMetadataSettings)
        {
            MergeData(source, target, Array.Empty<MetadataField>(), replaceData, mergeMetadataSettings);
        }

        public ItemUpdateType BeforeSave(Episode item)
        {
            return BeforeSaveInternal(item, false, ItemUpdateType.None);
        }
    }
}
