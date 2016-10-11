using System;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbum
    /// </summary>
    public class MusicAlbum : Folder, IHasAlbumArtist, IHasArtist, IHasMusicGenres, IHasLookupInfo<AlbumInfo>, IMetadataContainer
    {
        public List<string> AlbumArtists { get; set; }
        public List<string> Artists { get; set; }

        public MusicAlbum()
        {
            Artists = new List<string>();
            AlbumArtists = new List<string>();
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public MusicArtist MusicArtist
        {
            get
            {
                var artist = GetParents().OfType<MusicArtist>().FirstOrDefault();

                if (artist == null)
                {
                    var name = AlbumArtist;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        artist = LibraryManager.GetArtist(name);
                    }
                }
                return artist;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsCumulativeRunTimeTicks
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public List<string> AllArtists
        {
            get
            {
                var list = AlbumArtists.ToList();

                list.AddRange(Artists);

                return list;

            }
        }

        [IgnoreDataMember]
        public string AlbumArtist
        {
            get { return AlbumArtists.FirstOrDefault(); }
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the tracks.
        /// </summary>
        /// <value>The tracks.</value>
        [IgnoreDataMember]
        public IEnumerable<Audio> Tracks
        {
            get
            {
                return GetRecursiveChildren(i => i is Audio).Cast<Audio>();
            }
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Tracks;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (ConfigurationManager.Configuration.EnableStandaloneMusicKeys)
            {
                var albumArtist = AlbumArtist;
                if (!string.IsNullOrWhiteSpace(albumArtist))
                {
                    list.Insert(0, albumArtist + "-" + Name);
                }
            }

            var id = this.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrWhiteSpace(id))
            {
                list.Insert(0, "MusicAlbum-Musicbrainz-" + id);
            }

            id = this.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrWhiteSpace(id))
            {
                list.Insert(0, "MusicAlbum-MusicBrainzReleaseGroup-" + id);
            }

            return list;
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Music;
        }

        public AlbumInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AlbumInfo>();

            id.AlbumArtists = AlbumArtists;

            var artist = MusicArtist;

            if (artist != null)
            {
                id.ArtistProviderIds = artist.ProviderIds;
            }

            id.SongInfos = GetRecursiveChildren(i => i is Audio)
                .Cast<Audio>()
                .Select(i => i.GetLookupInfo())
                .ToList();

            var album = id.SongInfos
                .Select(i => i.Album)
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));

            if (!string.IsNullOrWhiteSpace(album))
            {
                id.Name = album;
            }

            return id;
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = GetRecursiveChildren().ToList();

            var totalItems = items.Count;
            var numComplete = 0;

            var childUpdateType = ItemUpdateType.None;

            // Refresh songs
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
                parentRefreshOptions = new MetadataRefreshOptions(refreshOptions);
                parentRefreshOptions.MetadataRefreshMode = MetadataRefreshMode.FullRefresh;
            }

            // Refresh current item
            await RefreshMetadata(parentRefreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }
    }
}
