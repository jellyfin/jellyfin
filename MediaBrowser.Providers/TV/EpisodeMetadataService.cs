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
    /// <summary>
    /// Service to manage episode metadata.
    /// </summary>
    public class EpisodeMetadataService : MetadataService<Episode, EpisodeInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SeasonMetadataService}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
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
        protected override ItemUpdateType BeforeSaveInternal(Episode item, bool isFullRefresh, ItemUpdateType updateType)
        {
            var updatedType = base.BeforeSaveInternal(item, isFullRefresh, updateType);

            var seriesName = item.FindSeriesName();
            if (!string.Equals(item.SeriesName, seriesName, StringComparison.Ordinal))
            {
                item.SeriesName = seriesName;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seasonName = item.FindSeasonName();
            if (!string.Equals(item.SeasonName, seasonName, StringComparison.Ordinal))
            {
                item.SeasonName = seasonName;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seriesId = item.FindSeriesId();
            if (!item.SeriesId.Equals(seriesId))
            {
                item.SeriesId = seriesId;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seasonId = item.FindSeasonId();
            if (!item.SeasonId.Equals(seasonId))
            {
                item.SeasonId = seasonId;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seriesPresentationUniqueKey = item.FindSeriesPresentationUniqueKey();
            if (!string.Equals(item.SeriesPresentationUniqueKey, seriesPresentationUniqueKey, StringComparison.Ordinal))
            {
                item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            return updatedType;
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Episode> source, MetadataResult<Episode> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

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

            if (replaceData || !targetItem.ParentIndexNumber.HasValue)
            {
                targetItem.ParentIndexNumber = sourceItem.ParentIndexNumber;
            }
        }
    }
}
