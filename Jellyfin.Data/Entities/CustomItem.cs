using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class CustomItem : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected CustomItem()
        {
            CustomItemMetadata = new HashSet<CustomItemMetadata>();
            Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static CustomItem CreateCustomItemUnsafe()
        {
            return new CustomItem();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public CustomItem(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.CustomItemMetadata = new HashSet<CustomItemMetadata>();
            this.Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static CustomItem Create(Guid urlid, DateTime dateadded)
        {
            return new CustomItem(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("CustomItemMetadata_CustomItemMetadata_Id")]
        public virtual ICollection<CustomItemMetadata> CustomItemMetadata { get; protected set; }

        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }

    }
}

