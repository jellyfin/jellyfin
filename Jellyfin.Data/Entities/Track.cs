using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Track : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Track()
        {
            // NOTE: This class has one-to-one associations with LibraryRoot, LibraryItem and CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            Releases = new HashSet<Release>();
            TrackMetadata = new HashSet<TrackMetadata>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Track CreateTrackUnsafe()
        {
            return new Track();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        /// <param name="_musicalbum0"></param>
        public Track(Guid urlid, DateTime dateadded, MusicAlbum _musicalbum0)
        {
            // NOTE: This class has one-to-one associations with LibraryRoot, LibraryItem and CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            this.UrlId = urlid;

            if (_musicalbum0 == null) throw new ArgumentNullException(nameof(_musicalbum0));
            _musicalbum0.Tracks.Add(this);

            this.Releases = new HashSet<Release>();
            this.TrackMetadata = new HashSet<TrackMetadata>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        /// <param name="_musicalbum0"></param>
        public static Track Create(Guid urlid, DateTime dateadded, MusicAlbum _musicalbum0)
        {
            return new Track(urlid, dateadded, _musicalbum0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for TrackNumber
        /// </summary>
        protected int? _TrackNumber;
        /// <summary>
        /// When provided in a partial class, allows value of TrackNumber to be changed before setting.
        /// </summary>
        partial void SetTrackNumber(int? oldValue, ref int? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of TrackNumber to be changed before returning.
        /// </summary>
        partial void GetTrackNumber(ref int? result);

        public int? TrackNumber
        {
            get
            {
                int? value = _TrackNumber;
                GetTrackNumber(ref value);
                return (_TrackNumber = value);
            }
            set
            {
                int? oldValue = _TrackNumber;
                SetTrackNumber(oldValue, ref value);
                if (oldValue != value)
                {
                    _TrackNumber = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }

        [ForeignKey("TrackMetadata_TrackMetadata_Id")]
        public virtual ICollection<TrackMetadata> TrackMetadata { get; protected set; }

    }
}

