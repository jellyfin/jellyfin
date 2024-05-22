#pragma warning disable CS1591

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
