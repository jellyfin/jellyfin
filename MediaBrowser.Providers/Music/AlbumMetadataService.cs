using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
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
    /// <summary>
    /// The album metadata service.
    /// </summary>
    public class AlbumMetadataService : MetadataService<MusicAlbum, AlbumInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
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
        protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(MusicAlbum item)
            => item.GetRecursiveChildren(i => i is Audio);

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(MusicAlbum item, IReadOnlyList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            // don't update user-changeable metadata for locked items
            if (item.IsLocked)
            {
                return updateType;
            }

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.LockedFields.Contains(MetadataField.Name))
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

                updateType |= SetArtistsFromSongs(item, songs);
                updateType |= SetAlbumArtistFromSongs(item, songs);
                updateType |= SetAlbumFromSongs(item, songs);
                updateType |= SetPeople(item);
            }

            return updateType;
        }

        private ItemUpdateType SetAlbumArtistFromSongs(MusicAlbum item, IReadOnlyList<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var albumArtists = songs
                .SelectMany(i => i.AlbumArtists)
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            updateType |= SetProviderIdFromSongs(item, songs, MetadataProvider.MusicBrainzAlbumArtist);

            if (!item.AlbumArtists.SequenceEqual(albumArtists, StringComparer.OrdinalIgnoreCase))
            {
                item.AlbumArtists = albumArtists;
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        private ItemUpdateType SetArtistsFromSongs(MusicAlbum item, IReadOnlyList<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var artists = songs
                .SelectMany(i => i.Artists)
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            if (!item.Artists.SequenceEqual(artists, StringComparer.OrdinalIgnoreCase))
            {
                item.Artists = artists;
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        private ItemUpdateType SetAlbumFromSongs(MusicAlbum item, IReadOnlyList<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            updateType |= SetProviderIdFromSongs(item, songs, MetadataProvider.MusicBrainzAlbum);
            updateType |= SetProviderIdFromSongs(item, songs, MetadataProvider.MusicBrainzReleaseGroup);

            return updateType;
        }

        private ItemUpdateType SetProviderIdFromSongs(BaseItem item, IReadOnlyList<Audio> songs, MetadataProvider provider)
        {
            var ids = songs
                .Select(i => i.GetProviderId(provider))
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            var id = item.GetProviderId(provider);
            if (ids.Length != 0)
            {
                var firstId = ids[0];
                if (!string.IsNullOrEmpty(firstId)
                    && (string.IsNullOrEmpty(id)
                        || !id.Equals(firstId, StringComparison.OrdinalIgnoreCase)))
                {
                    item.SetProviderId(provider, firstId);
                    return ItemUpdateType.MetadataEdit;
                }
            }

            return ItemUpdateType.None;
        }

        private void SetProviderId(MusicAlbum sourceItem, MusicAlbum targetItem, MetadataProvider provider)
        {
            var source = sourceItem.GetProviderId(provider);
            var target = targetItem.GetProviderId(provider);
            if (!string.IsNullOrEmpty(source)
                && (string.IsNullOrEmpty(target)
                    || !target.Equals(source, StringComparison.Ordinal)))
            {
                targetItem.SetProviderId(provider, source);
            }
        }

        private ItemUpdateType SetPeople(MusicAlbum item)
        {
            var updateType = ItemUpdateType.None;

            if (item.AlbumArtists.Any() || item.Artists.Any())
            {
                var people = new List<PersonInfo>();

                foreach (var albumArtist in item.AlbumArtists)
                {
                    PeopleHelper.AddPerson(people, new PersonInfo
                    {
                        Name = albumArtist,
                        Type = PersonKind.AlbumArtist
                    });
                }

                foreach (var artist in item.Artists)
                {
                    PeopleHelper.AddPerson(people, new PersonInfo
                    {
                        Name = artist,
                        Type = PersonKind.Artist
                    });
                }

                LibraryManager.UpdatePeople(item, people);
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        /// <inheritdoc />
        protected override void MergeData(
            MetadataResult<MusicAlbum> source,
            MetadataResult<MusicAlbum> target,
            MetadataField[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
            else
            {
                targetItem.Artists = targetItem.Artists.Concat(sourceItem.Artists).Distinct().ToArray();
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist)))
            {
                SetProviderId(sourceItem, targetItem, MetadataProvider.MusicBrainzAlbumArtist);
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbum)))
            {
                SetProviderId(sourceItem, targetItem, MetadataProvider.MusicBrainzAlbum);
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup)))
            {
                SetProviderId(sourceItem, targetItem, MetadataProvider.MusicBrainzReleaseGroup);
            }
        }
    }
}
