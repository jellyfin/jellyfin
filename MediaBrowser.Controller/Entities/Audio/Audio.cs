using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Audio
    /// </summary>
    public class Audio : BaseItem, IHasMediaStreams, IHasAlbumArtist, IHasArtist, IHasMusicGenres, IHasLookupInfo<SongInfo>, IHasSeries
    {
        public Audio()
        {
            Artists = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has embedded image.
        /// </summary>
        /// <value><c>true</c> if this instance has embedded image; otherwise, <c>false</c>.</value>
        public bool HasEmbeddedImage { get; set; }

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
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return Parents.OfType<MusicAlbum>().FirstOrDefault() ?? new MusicAlbum { Name = "<Unknown>" };
            }
        }

        [IgnoreDataMember]
        public string SeriesName
        {
            get
            {
                return Album;
            }
        }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        public List<string> Artists { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }
        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Audio;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                    + (IndexNumber != null ? IndexNumber.Value.ToString("0000 - ") : "") + Name;
        }

        /// <summary>
        /// Determines whether the specified name has artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name has artist; otherwise, <c>false</c>.</returns>
        public bool HasArtist(string name)
        {
            return Artists.Contains(name, StringComparer.OrdinalIgnoreCase) || string.Equals(AlbumArtist, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            var parent = FindParent<MusicAlbum>();

            if (parent != null)
            {
                var parentKey = parent.GetUserDataKey();

                if (IndexNumber.HasValue)
                {
                    var songKey = (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                                  + (IndexNumber.Value.ToString("0000 - "));

                    return parentKey + songKey;
                }
            }

            return base.GetUserDataKey();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedMusic;
        }

        public SongInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SongInfo>();

            info.AlbumArtist = AlbumArtist;
            info.Album = Album;
            info.Artists = Artists;

            return info;
        }
    }
}
