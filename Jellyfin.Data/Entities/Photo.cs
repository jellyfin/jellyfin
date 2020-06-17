using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Photo : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Photo()
        {
            PhotoMetadata = new HashSet<PhotoMetadata>();
            Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Photo CreatePhotoUnsafe()
        {
            return new Photo();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public Photo(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.PhotoMetadata = new HashSet<PhotoMetadata>();
            this.Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static Photo Create(Guid urlid, DateTime dateadded)
        {
            return new Photo(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("PhotoMetadata_PhotoMetadata_Id")]
        public virtual ICollection<PhotoMetadata> PhotoMetadata { get; protected set; }

        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }

    }
}

