using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Jellyfin.Data.Entities
{
    [Table("MusicAlbum")]
    public partial class MusicAlbum : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected MusicAlbum() : base()
        {
            MusicAlbumMetadata = new HashSet<MusicAlbumMetadata>();
            Tracks = new HashSet<Track>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static MusicAlbum CreateMusicAlbumUnsafe()
        {
            return new MusicAlbum();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public MusicAlbum(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.MusicAlbumMetadata = new HashSet<MusicAlbumMetadata>();
            this.Tracks = new HashSet<Track>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static MusicAlbum Create(Guid urlid, DateTime dateadded)
        {
            return new MusicAlbum(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("MusicAlbumMetadata_MusicAlbumMetadata_Id")]
        public virtual ICollection<MusicAlbumMetadata> MusicAlbumMetadata { get; protected set; }

        [ForeignKey("Track_Tracks_Id")]
        public virtual ICollection<Track> Tracks { get; protected set; }

    }
}

