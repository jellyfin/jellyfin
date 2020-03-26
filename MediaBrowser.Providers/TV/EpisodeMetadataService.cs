using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeMetadataService : MetadataService<Episode, EpisodeInfo>
    {
        public EpisodeMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<EpisodeMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override ItemUpdateType BeforeSaveInternal(Episode item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.BeforeSaveInternal(item, isFullRefresh, currentUpdateType);

            var seriesName = item.FindSeriesName();
            if (!string.Equals(item.SeriesName, seriesName, StringComparison.Ordinal))
            {
                item.SeriesName = seriesName;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seasonName = item.FindSeasonName();
            if (!string.Equals(item.SeasonName, seasonName, StringComparison.Ordinal))
            {
                item.SeasonName = seasonName;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seriesId = item.FindSeriesId();
            if (!item.SeriesId.Equals(seriesId))
            {
                item.SeriesId = seriesId;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seasonId = item.FindSeasonId();
            if (!item.SeasonId.Equals(seasonId))
            {
                item.SeasonId = seasonId;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seriesPresentationUniqueKey = item.FindSeriesPresentationUniqueKey();
            if (!string.Equals(item.SeriesPresentationUniqueKey, seriesPresentationUniqueKey, StringComparison.Ordinal))
            {
                item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Episode> source, MetadataResult<Episode> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
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

            if (replaceData || !targetItem.IndexNumberEnd.HasValue)
            {
                targetItem.IndexNumberEnd = sourceItem.IndexNumberEnd;
            }
        }
    }
}
