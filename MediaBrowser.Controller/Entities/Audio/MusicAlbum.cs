using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbum
    /// </summary>
    public class MusicAlbum : Folder, IHasAlbumArtist, IHasArtist, IHasMusicGenres, IHasLookupInfo<AlbumInfo>
    {
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
                return Parents.OfType<MusicArtist>().FirstOrDefault();
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

        public List<string> AlbumArtists { get; set; }

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

        public List<string> Artists { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateUserDataKey()
        {
            var id = this.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrWhiteSpace(id))
            {
                return "MusicAlbum-MusicBrainzReleaseGroup-" + id;
            }

            id = this.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrWhiteSpace(id))
            {
                return "MusicAlbum-Musicbrainz-" + id;
            }

            return base.CreateUserDataKey();
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public AlbumInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AlbumInfo>();

            id.AlbumArtists = AlbumArtists;

            var artist = Parents.OfType<MusicArtist>().FirstOrDefault();

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
    }
}
