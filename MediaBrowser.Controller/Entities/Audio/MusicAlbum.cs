using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbum
    /// </summary>
    public class MusicAlbum : Folder, IHasAlbumArtist, IHasArtist, IHasMusicGenres, IHasTags, IHasLookupInfo<AlbumInfo>, IHasSeries
    {
        public List<Guid> SoundtrackIds { get; set; }

        public MusicAlbum()
        {
            Artists = new List<string>();
            SoundtrackIds = new List<Guid>();
            Tags = new List<string>();
        }

        [IgnoreDataMember]
        public MusicArtist MusicArtist
        {
            get
            {
                return Parents.OfType<MusicArtist>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Songs will group into us so don't also include us in the index
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IncludeInIndex
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public string SeriesName
        {
            get
            {
                return AlbumArtist;
            }
        }

        /// <summary>
        /// Override this to true if class should be grouped under a container in indicies
        /// The container class should be defined via IndexContainer
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The unknwon artist
        /// </summary>
        private static readonly MusicArtist UnknwonArtist = new MusicArtist { Name = "<Unknown>" };

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get { return Parent as MusicArtist ?? UnknwonArtist; }
        }

        /// <summary>
        /// Determines whether the specified artist has artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <returns><c>true</c> if the specified artist has artist; otherwise, <c>false</c>.</returns>
        public bool HasArtist(string artist)
        {
            return string.Equals(AlbumArtist, artist, StringComparison.OrdinalIgnoreCase)
                   || Artists.Contains(artist, StringComparer.OrdinalIgnoreCase);
        }

        public string AlbumArtist { get; set; }

        public List<string> Artists { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            var id = this.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(id))
            {
                return "MusicAlbum-MusicBrainzReleaseGroup-" + id;
            }

            id = this.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(id))
            {
                return "MusicAlbum-Musicbrainz-" + id;
            }

            return base.GetUserDataKey();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public AlbumInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AlbumInfo>();

            id.AlbumArtist = AlbumArtist;

            var artist = Parents.OfType<MusicArtist>().FirstOrDefault();

            if (artist != null)
            {
                id.ArtistProviderIds = artist.ProviderIds;
            }

            id.SongInfos = RecursiveChildren.OfType<Audio>()
                .Select(i => i.GetLookupInfo())
                .ToList();

            return id;
        }
    }

    public class MusicAlbumDisc : Folder
    {

    }
}
