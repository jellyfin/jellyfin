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
    public class SeasonMetadataService : MetadataService<Season, SeasonInfo>
    {
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
        protected override ItemUpdateType BeforeSaveInternal(Season item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.BeforeSaveInternal(item, isFullRefresh, currentUpdateType);

            if (item.IndexNumber.HasValue && item.IndexNumber.Value == 0)
            {
                var seasonZeroDisplayName = LibraryManager.GetLibraryOptions(item).SeasonZeroDisplayName;

                if (!string.Equals(item.Name, seasonZeroDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = seasonZeroDisplayName;
                    updateType = updateType | ItemUpdateType.MetadataEdit;
                }
            }

            var seriesName = item.FindSeriesName();
            if (!string.Equals(item.SeriesName, seriesName, StringComparison.Ordinal))
            {
                item.SeriesName = seriesName;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seriesPresentationUniqueKey = item.FindSeriesPresentationUniqueKey();
            if (!string.Equals(item.SeriesPresentationUniqueKey, seriesPresentationUniqueKey, StringComparison.Ordinal))
            {
                item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
                updateType |= ItemUpdateType.MetadataImport;
            }

            var seriesId = item.FindSeriesId();
            if (!item.SeriesId.Equals(seriesId))
            {
                item.SeriesId = seriesId;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingPremiereDateFromChildren => true;

        /// <inheritdoc />
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(Season item)
            => item.GetEpisodes();

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(Season item, IList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                updateType |= SaveIsVirtualItem(item, children);
            }

            return updateType;
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Season> source, MetadataResult<Season> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        private ItemUpdateType SaveIsVirtualItem(Season item, IList<BaseItem> episodes)
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
