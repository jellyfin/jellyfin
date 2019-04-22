using System.Collections.Generic;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Playlists;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.Playlists
{
    public class PlaylistMetadataService : MetadataService<Playlist, ItemLookupInfo>
    {
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(Playlist item)
        {
            return item.GetLinkedChildren();
        }

        protected override void MergeData(MetadataResult<Playlist> source, MetadataResult<Playlist> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (mergeMetadataSettings)
            {
                targetItem.PlaylistMediaType = sourceItem.PlaylistMediaType;
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
                targetItem.Shares = sourceItem.Shares;
            }
        }

        public PlaylistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }

        protected override bool EnableUpdatingGenresFromChildren => true;

        protected override bool EnableUpdatingOfficialRatingFromChildren => true;

        protected override bool EnableUpdatingStudiosFromChildren => true;
    }
}
