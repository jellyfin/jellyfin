using System.Collections.Generic;
using System.Linq;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.BoxSets
{
    public class BoxSetMetadataService : MetadataService<BoxSet, BoxSetInfo>
    {
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(BoxSet item)
        {
            return item.GetLinkedChildren();
        }

        protected override void MergeData(MetadataResult<BoxSet> source, MetadataResult<BoxSet> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (mergeMetadataSettings)
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
            }
        }

        protected override ItemUpdateType BeforeSaveInternal(BoxSet item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.BeforeSaveInternal(item, isFullRefresh, currentUpdateType);

            var libraryFolderIds = item.GetLibraryFolderIds();

            var itemLibraryFolderIds = item.LibraryFolderIds;
            if (itemLibraryFolderIds == null || !libraryFolderIds.SequenceEqual(itemLibraryFolderIds))
            {
                item.LibraryFolderIds = libraryFolderIds;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        public BoxSetMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }

        protected override bool EnableUpdatingGenresFromChildren => true;

        protected override bool EnableUpdatingOfficialRatingFromChildren => true;

        protected override bool EnableUpdatingStudiosFromChildren => true;

        protected override bool EnableUpdatingPremiereDateFromChildren => true;
    }
}
