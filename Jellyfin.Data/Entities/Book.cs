using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Book : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Book()
        {
            BookMetadata = new HashSet<BookMetadata>();
            Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Book CreateBookUnsafe()
        {
            return new Book();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public Book(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.BookMetadata = new HashSet<BookMetadata>();
            this.Releases = new HashSet<Release>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static Book Create(Guid urlid, DateTime dateadded)
        {
            return new Book(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("BookMetadata_BookMetadata_Id")]
        public virtual ICollection<BookMetadata> BookMetadata { get; protected set; }

        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }

    }
}

