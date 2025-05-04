using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
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
    /// Service to manage season metadata.
    /// </summary>
    public class SeasonMetadataService : MetadataService<Season, SeasonInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SeasonMetadataService}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public SeasonMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<SeasonMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingPremiereDateFromChildren => true;

        /// <inheritdoc />
        protected override ItemUpdateType BeforeSaveInternal(Season item, bool isFullRefresh, ItemUpdateType updateType)
        {
            var updatedType = base.BeforeSaveInternal(item, isFullRefresh, updateType);

            if (item.IndexNumber == 0 && !item.IsLocked && !item.LockedFields.Contains(MetadataField.Name))
            {
                var seasonZeroDisplayName = LibraryManager.GetLibraryOptions(item).SeasonZeroDisplayName;

                if (!string.Equals(item.Name, seasonZeroDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = seasonZeroDisplayName;
                    updatedType |= ItemUpdateType.MetadataEdit;
                }
            }

            var seriesName = item.FindSeriesName();
            if (!string.Equals(item.SeriesName, seriesName, StringComparison.Ordinal))
            {
                item.SeriesName = seriesName;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seriesPresentationUniqueKey = item.FindSeriesPresentationUniqueKey();
            if (!string.Equals(item.SeriesPresentationUniqueKey, seriesPresentationUniqueKey, StringComparison.Ordinal))
            {
                item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            var seriesId = item.FindSeriesId();
            if (!item.SeriesId.Equals(seriesId))
            {
                item.SeriesId = seriesId;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            return updatedType;
        }

        /// <inheritdoc />
        protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(Season item)
            => item.GetEpisodes();

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(Season item, IReadOnlyList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                updateType |= SaveIsVirtualItem(item, children);
            }

            return updateType;
        }

        private ItemUpdateType SaveIsVirtualItem(Season item, IReadOnlyList<BaseItem> episodes)
        {
            var isVirtualItem = item.LocationType == LocationType.Virtual && (episodes.Count == 0 || episodes.All(i => i.LocationType == LocationType.Virtual));

            if (item.IsVirtualItem != isVirtualItem)
            {
                item.IsVirtualItem = isVirtualItem;
                return ItemUpdateType.MetadataEdit;
            }

            return ItemUpdateType.None;
        }
    }
}
