using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    public class AlbumMetadataService : MetadataService<MusicAlbum, AlbumInfo>
    {
        public AlbumMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<AlbumMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingPremiereDateFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingGenresFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingStudiosFromChildren => true;

        /// <inheritdoc />
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicAlbum item)
            => item.GetRecursiveChildren(i => i is Audio);

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(MusicAlbum item, IList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.LockedFields.Contains(MetadataFields.Name))
                {
                    var name = children.Select(i => i.Album).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                    if (!string.IsNullOrEmpty(name)
                        && !string.Equals(item.Name, name, StringComparison.Ordinal))
                    {
                        item.Name = name;
                        updateType |= ItemUpdateType.MetadataEdit;
                    }
                }

                var songs = children.Cast<Audio>().ToArray();

                updateType |= SetAlbumArtistFromSongs(item, songs);
                updateType |= SetArtistsFromSongs(item, songs);
            }

            return updateType;
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

        /// <inheritdoc />
        protected override void MergeData(
            MetadataResult<MusicAlbum> source,
            MetadataResult<MusicAlbum> target,
            MetadataFields[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
        }
    }
}
