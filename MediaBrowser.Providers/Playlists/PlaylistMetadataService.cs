using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Providers.Playlists
{
    class PlaylistMetadataService : MetadataService<Playlist, ItemLookupInfo>
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

        protected override bool EnableUpdatingGenresFromChildren
        {
            get
            {
                return true;
            }
        }

        protected override bool EnableUpdatingOfficialRatingFromChildren
        {
            get
            {
                return true;
            }
        }

        protected override bool EnableUpdatingStudiosFromChildren
        {
            get
            {
                return true;
            }
        }
    }
}
