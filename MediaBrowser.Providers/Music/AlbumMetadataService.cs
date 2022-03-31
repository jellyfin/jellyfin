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
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicAlbum item)
            => item.GetRecursiveChildren(i => i is Audio);

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(MusicAlbum item, IList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

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

            var musicbrainzAlbumArtistIds = songs
                .Select(i => i.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist))
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            var musicbrainzAlbumArtistId = item.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);
            var firstMusicbrainzAlbumArtistId = musicbrainzAlbumArtistIds[0];
            if (!string.IsNullOrEmpty(firstMusicbrainzAlbumArtistId) &&
                (string.IsNullOrEmpty(musicbrainzAlbumArtistId)
                    || !musicbrainzAlbumArtistId.Equals(firstMusicbrainzAlbumArtistId, StringComparison.OrdinalIgnoreCase)))
            {
                item.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, firstMusicbrainzAlbumArtistId);
                updateType |= ItemUpdateType.MetadataEdit;
            }

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

            var musicbrainzAlbumIds = songs
                .Select(i => i.GetProviderId(MetadataProvider.MusicBrainzAlbum))
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            var musicbrainzAlbumId = item.GetProviderId(MetadataProvider.MusicBrainzAlbum);
            if (!String.IsNullOrEmpty(musicbrainzAlbumIds[0])
                && (String.IsNullOrEmpty(musicbrainzAlbumId)
                    || !musicbrainzAlbumId.Equals(musicbrainzAlbumIds[0], StringComparison.OrdinalIgnoreCase)))
            {
                item.SetProviderId(MetadataProvider.MusicBrainzAlbum, musicbrainzAlbumIds[0]!);
                updateType |= ItemUpdateType.MetadataEdit;
            }

            var musicbrainzReleaseGroupIds = songs
                .Select(i => i.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup))
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToArray();

            var musicbrainzReleaseGroupId = item.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);
            if (!String.IsNullOrEmpty(musicbrainzReleaseGroupIds[0])
                && (String.IsNullOrEmpty(musicbrainzReleaseGroupId)
                    || !musicbrainzReleaseGroupId.Equals(musicbrainzReleaseGroupIds[0], StringComparison.OrdinalIgnoreCase)))
            {
                item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, musicbrainzReleaseGroupIds[0]!);
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
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
                        Type = "AlbumArtist"
                    });
                }

                foreach (var artist in item.Artists)
                {
                    PeopleHelper.AddPerson(people, new PersonInfo
                    {
                        Name = artist,
                        Type = "Artist"
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

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist)))
            {
                var targetAlbumArtistId = targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);
                var sourceAlbumArtistId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);

                if (!string.IsNullOrEmpty(sourceAlbumArtistId)
                    && (string.IsNullOrEmpty(targetAlbumArtistId)
                        || !targetAlbumArtistId.Equals(sourceAlbumArtistId, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, sourceAlbumArtistId);
                }
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbum)))
            {
                var targetAlbumId = targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbum);
                var sourceAlbumId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzAlbum);

                if (!string.IsNullOrEmpty(sourceAlbumId)
                    && (string.IsNullOrEmpty(targetAlbumId)
                        || !targetAlbumId.Equals(sourceAlbumId, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzAlbum, sourceAlbumId);
                }
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup)))
            {
                var targetReleaseGroupId = targetItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);
                var sourceReleaseGroupId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

                if (!string.IsNullOrEmpty(sourceReleaseGroupId)
                    && (string.IsNullOrEmpty(targetReleaseGroupId)
                        || !targetReleaseGroupId.Equals(sourceItem)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, sourceReleaseGroupId);
                }
            }
        }
    }
}
