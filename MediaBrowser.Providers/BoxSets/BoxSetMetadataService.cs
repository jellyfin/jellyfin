#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.BoxSets
{
    public class BoxSetMetadataService : MetadataService<BoxSet, BoxSetInfo>
    {
        public BoxSetMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<BoxSetMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingGenresFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingOfficialRatingFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingStudiosFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingPremiereDateFromChildren => true;

        /// <inheritdoc />
        protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(BoxSet item)
        {
            return item.GetLinkedChildren();
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<BoxSet> source, MetadataResult<BoxSet> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (mergeMetadataSettings)
            {
                if (replaceData || targetItem.LinkedChildren.Length == 0)
                {
                    targetItem.LinkedChildren = sourceItem.LinkedChildren;
                }
                else
                {
                    targetItem.LinkedChildren = sourceItem.LinkedChildren.Concat(targetItem.LinkedChildren).Distinct().ToArray();
                }
            }
        }

        /// <inheritdoc />
        protected override ItemUpdateType BeforeSaveInternal(BoxSet item, bool isFullRefresh, ItemUpdateType updateType)
        {
            var updatedType = base.BeforeSaveInternal(item, isFullRefresh, updateType);

            var libraryFolderIds = item.GetLibraryFolderIds();

            var itemLibraryFolderIds = item.LibraryFolderIds;
            if (itemLibraryFolderIds is null || !libraryFolderIds.SequenceEqual(itemLibraryFolderIds))
            {
                item.LibraryFolderIds = libraryFolderIds;
                updatedType |= ItemUpdateType.MetadataImport;
            }

            return updatedType;
        }
    }
}
