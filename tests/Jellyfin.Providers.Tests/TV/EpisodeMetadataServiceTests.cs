using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
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

    public EpisodeMetadataServiceTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        BaseItem.LibraryManager = Mock.Of<ILibraryManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
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
    [InlineData(2, 1)] // e.g. an nfo with its episodedetails blocks in descending order
    [InlineData(22, 21)]
    public void BeforeSave_ReversedEpisodeRange_RestoresOrder(int indexNumber, int indexNumberEnd)
    {
        var item = new Episode
        {
            IndexNumber = indexNumber,
            IndexNumberEnd = indexNumberEnd
        };

        var updateType = _service.BeforeSave(item);

        // The range still covers the same episodes, it is just no longer transposed
        Assert.Equal(indexNumberEnd, item.IndexNumber);
        Assert.Equal(indexNumber, item.IndexNumberEnd);
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

    private sealed class TestEpisodeMetadataService : EpisodeMetadataService
    {
        public TestEpisodeMetadataService()
            : base(
                Mock.Of<IServerConfigurationManager>(),
                NullLogger<EpisodeMetadataService>.Instance,
                Mock.Of<IProviderManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<ILibraryManager>(),
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
