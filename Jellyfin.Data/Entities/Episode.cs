using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Episode : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Episode()
        {
            // NOTE: This class has one-to-one associations with LibraryRoot, LibraryItem and CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            Releases = new HashSet<Release>();
            EpisodeMetadata = new HashSet<EpisodeMetadata>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Episode CreateEpisodeUnsafe()
        {
            return new Episode();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        /// <param name="_season0"></param>
        public Episode(Guid urlid, DateTime dateadded, Season _season0)
        {
            // NOTE: This class has one-to-one associations with LibraryRoot, LibraryItem and CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            this.UrlId = urlid;

            if (_season0 == null) throw new ArgumentNullException(nameof(_season0));
            _season0.Episodes.Add(this);

            this.Releases = new HashSet<Release>();
            this.EpisodeMetadata = new HashSet<EpisodeMetadata>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        /// <param name="_season0"></param>
        public static Episode Create(Guid urlid, DateTime dateadded, Season _season0)
        {
            return new Episode(urlid, dateadded, _season0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for EpisodeNumber
        /// </summary>
        protected int? _EpisodeNumber;
        /// <summary>
        /// When provided in a partial class, allows value of EpisodeNumber to be changed before setting.
        /// </summary>
        partial void SetEpisodeNumber(int? oldValue, ref int? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of EpisodeNumber to be changed before returning.
        /// </summary>
        partial void GetEpisodeNumber(ref int? result);

        public int? EpisodeNumber
        {
            get
            {
                int? value = _EpisodeNumber;
                GetEpisodeNumber(ref value);
                return (_EpisodeNumber = value);
            }
            set
            {
                int? oldValue = _EpisodeNumber;
                SetEpisodeNumber(oldValue, ref value);
                if (oldValue != value)
                {
                    _EpisodeNumber = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }
        [ForeignKey("EpisodeMetadata_EpisodeMetadata_Id")]
        public virtual ICollection<EpisodeMetadata> EpisodeMetadata { get; protected set; }

    }
}

