using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using CommonIO;

namespace MediaBrowser.Providers.Playlists
{
    class PlaylistMetadataService : MetadataService<Playlist, ItemLookupInfo>
    {
        protected override void MergeData(MetadataResult<Playlist> source, MetadataResult<Playlist> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.PlaylistMediaType))
            {
                targetItem.PlaylistMediaType = sourceItem.PlaylistMediaType;
            }

            if (mergeMetadataSettings)
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
                targetItem.Shares = sourceItem.Shares;
            }
        }

        public PlaylistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
