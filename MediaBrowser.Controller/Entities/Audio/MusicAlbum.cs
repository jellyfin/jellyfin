#nullable disable

#pragma warning disable CA1721, CA1826, CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbum.
    /// </summary>
    [Common.RequiresSourceSerialisation]
    public class MusicAlbum : Folder, IHasAlbumArtist, IHasArtist, IHasMusicGenres, IHasLookupInfo<AlbumInfo>, IMetadataContainer
    {
        public MusicAlbum()
        {
            Artists = Array.Empty<string>();
            AlbumArtists = Array.Empty<string>();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> AlbumArtists { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<string> Artists { get; set; }

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public MusicArtist MusicArtist => GetMusicArtist(new DtoOptions(true));

        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        [JsonIgnore]
        public override bool SupportsCumulativeRunTimeTicks => true;

        [JsonIgnore]
        public string AlbumArtist => AlbumArtists.FirstOrDefault();

        [JsonIgnore]
        public override bool SupportsPeople => true;

        /// <summary>
        /// Gets the tracks.
        /// </summary>
        /// <value>The tracks.</value>
        [JsonIgnore]
        public IEnumerable<Audio> Tracks => GetRecursiveChildren(i => i is Audio).Cast<Audio>();

        public MusicArtist GetMusicArtist(DtoOptions options)
        {
            var parents = GetParents();
            foreach (var parent in parents)
            {
                if (parent is MusicArtist artist)
                {
                    return artist;
                }
            }

            var name = AlbumArtist;
            if (!string.IsNullOrEmpty(name))
            {
                return LibraryManager.GetArtist(name, options);
            }

            return null;
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Tracks;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var albumArtist = AlbumArtist;
            if (!string.IsNullOrEmpty(albumArtist))
            {
                list.Insert(0, albumArtist + "-" + Name);
            }

            var id = this.GetProviderId(MetadataProvider.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(id))
            {
                list.Insert(0, "MusicAlbum-Musicbrainz-" + id);
            }

            id = this.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(id))
            {
                list.Insert(0, "MusicAlbum-MusicBrainzReleaseGroup-" + id);
            }

            return list;
        }

        protected override bool GetBlockUnratedValue(User user)
        {
            return user.GetPreferenceValues<UnratedItem>(PreferenceKind.BlockUnratedItems).Contains(UnratedItem.Music);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Music;
        }

        public AlbumInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AlbumInfo>();

            id.AlbumArtists = AlbumArtists;

            var artist = GetMusicArtist(new DtoOptions(false));

            if (artist is not null)
            {
                id.ArtistProviderIds = artist.ProviderIds;
            }

            id.SongInfos = GetRecursiveChildren(i => i is Audio)
                .Cast<Audio>()
                .Select(i => i.GetLookupInfo())
                .ToList();

            var album = id.SongInfos
                .Select(i => i.Album)
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            if (!string.IsNullOrEmpty(album))
            {
                id.Name = album;
            }

            return id;
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = GetRecursiveChildren();

            var totalItems = items.Count;
            var numComplete = 0;

            var childUpdateType = ItemUpdateType.None;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updateType = await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                childUpdateType = childUpdateType | updateType;

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 95);
            }

            var parentRefreshOptions = refreshOptions;
            if (childUpdateType > ItemUpdateType.None)
            {
                parentRefreshOptions = new MetadataRefreshOptions(refreshOptions)
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                };
            }

            // Refresh current item
            await RefreshMetadata(parentRefreshOptions, cancellationToken).ConfigureAwait(false);

            if (!refreshOptions.IsAutomated)
            {
                await RefreshArtists(refreshOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshArtists(MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            foreach (var i in this.GetAllArtists())
            {
                // This should not be necessary but we're seeing some cases of it
                if (string.IsNullOrEmpty(i))
                {
                    continue;
                }

                var artist = LibraryManager.GetArtist(i);

                if (!artist.IsAccessedByName)
                {
                    continue;
                }

                await artist.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
