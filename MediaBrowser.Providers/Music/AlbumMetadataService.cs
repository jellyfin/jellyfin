using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Providers.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Providers.Music
{
    public class AlbumMetadataService : MetadataService<MusicAlbum, AlbumInfo>
    {
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicAlbum item)
        {
            return item.GetRecursiveChildren(i => i is Audio)
                        .ToList();
        }

        protected override ItemUpdateType UpdateMetadataFromChildren(MusicAlbum item, IList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.LockedFields.Contains(MetadataFields.Name))
                {
                    var name = children.Select(i => i.Album).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!string.Equals(item.Name, name, StringComparison.Ordinal))
                        {
                            item.Name = name;
                            updateType = updateType | ItemUpdateType.MetadataEdit;
                        }
                    }
                }

                var songs = children.Cast<Audio>().ToArray();

                updateType = updateType | SetAlbumArtistFromSongs(item, songs);
                updateType = updateType | SetArtistsFromSongs(item, songs);
            }

            return updateType;
        }

        protected override bool EnableUpdatingPremiereDateFromChildren
        {
            get
            {
                return true;
            }
        }

        protected override bool EnableUpdatingGenresFromChildren
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

        private ItemUpdateType SetAlbumArtistFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;
            
            var artists = songs
                .SelectMany(i => i.AlbumArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            if (!item.AlbumArtists.SequenceEqual(artists, StringComparer.OrdinalIgnoreCase))
            {
                item.AlbumArtists = artists;
                updateType = updateType | ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        private ItemUpdateType SetArtistsFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var artists = songs
                .SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            if (!item.Artists.SequenceEqual(artists, StringComparer.OrdinalIgnoreCase))
            {
                item.Artists = artists;
                updateType = updateType | ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        protected override void MergeData(MetadataResult<MusicAlbum> source, MetadataResult<MusicAlbum> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Length == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
        }

        public AlbumMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
