using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeMetadataService : MetadataService<Episode, EpisodeInfo>
    {
        protected override async Task<ItemUpdateType> BeforeSave(Episode item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = await base.BeforeSave(item, isFullRefresh, currentUpdateType).ConfigureAwait(false);

            if (updateType <= ItemUpdateType.None)
            {
                if (!string.Equals(item.SeriesName, item.FindSeriesName(), StringComparison.Ordinal))
                {
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }
            if (updateType <= ItemUpdateType.None)
            {
                if (!string.Equals(item.SeriesSortName, item.FindSeriesSortName(), StringComparison.Ordinal))
                {
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }
            if (updateType <= ItemUpdateType.None)
            {
                if (!string.Equals(item.SeasonName, item.FindSeasonName(), StringComparison.Ordinal))
                {
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }
            if (updateType <= ItemUpdateType.None)
            {
                if (item.SeriesId != item.FindSeriesId())
                {
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }
            if (updateType <= ItemUpdateType.None)
            {
                if (item.SeasonId != item.FindSeasonId())
                {
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }

        protected override void MergeData(MetadataResult<Episode> source, MetadataResult<Episode> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || !targetItem.AirsBeforeSeasonNumber.HasValue)
            {
                targetItem.AirsBeforeSeasonNumber = sourceItem.AirsBeforeSeasonNumber;
            }

            if (replaceData || !targetItem.AirsAfterSeasonNumber.HasValue)
            {
                targetItem.AirsAfterSeasonNumber = sourceItem.AirsAfterSeasonNumber;
            }

            if (replaceData || !targetItem.AirsBeforeEpisodeNumber.HasValue)
            {
                targetItem.AirsBeforeEpisodeNumber = sourceItem.AirsBeforeEpisodeNumber;
            }

            if (replaceData || !targetItem.DvdSeasonNumber.HasValue)
            {
                targetItem.DvdSeasonNumber = sourceItem.DvdSeasonNumber;
            }

            if (replaceData || !targetItem.DvdEpisodeNumber.HasValue)
            {
                targetItem.DvdEpisodeNumber = sourceItem.DvdEpisodeNumber;
            }

            if (replaceData || !targetItem.AbsoluteEpisodeNumber.HasValue)
            {
                targetItem.AbsoluteEpisodeNumber = sourceItem.AbsoluteEpisodeNumber;
            }

            if (replaceData || !targetItem.IndexNumberEnd.HasValue)
            {
                targetItem.IndexNumberEnd = sourceItem.IndexNumberEnd;
            }
        }

        public EpisodeMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
