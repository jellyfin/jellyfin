using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

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

        protected override ItemUpdateType BeforeSave(Playlist item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.BeforeSave(item, isFullRefresh, currentUpdateType);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.IsLocked && !item.LockedFields.Contains(MetadataFields.Genres))
                {
                    var items = item.GetLinkedChildren()
                        .ToList();

                    var currentList = item.Genres.ToList();

                    item.Genres = items.SelectMany(i => i.Genres)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (currentList.Count != item.Genres.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Genres.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                    {
                        updateType = updateType | ItemUpdateType.MetadataEdit;
                    }
                }
            }

            return updateType;
        }

        public PlaylistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
