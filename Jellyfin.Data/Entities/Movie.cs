using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Movie : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Movie()
        {
            Releases = new HashSet<Release>();
            MovieMetadata = new HashSet<MovieMetadata>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Movie CreateMovieUnsafe()
        {
            return new Movie();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public Movie(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.Releases = new HashSet<Release>();
            this.MovieMetadata = new HashSet<MovieMetadata>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static Movie Create(Guid urlid, DateTime dateadded)
        {
            return new Movie(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("Release_Releases_Id")]
        public virtual ICollection<Release> Releases { get; protected set; }

        [ForeignKey("MovieMetadata_MovieMetadata_Id")]
        public virtual ICollection<MovieMetadata> MovieMetadata { get; protected set; }

    }
}

