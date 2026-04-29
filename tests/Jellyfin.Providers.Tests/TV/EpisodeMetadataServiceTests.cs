using System;
using MediaBrowser.Controller.Configuration;
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

public class EpisodeMetadataServiceTests
{
    private readonly TestEpisodeMetadataService _service = new();

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
    }
}
