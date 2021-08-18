using System.Collections.Generic;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a season.
    /// </summary>
    public class Season : LibraryItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Season"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public Season(Library library) : base(library)
        {
            Episodes = new HashSet<Episode>();
            SeasonMetadata = new HashSet<SeasonMetadata>();
        }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets the season metadata.
        /// </summary>
        public virtual ICollection<SeasonMetadata> SeasonMetadata { get; private set; }

        /// <summary>
        /// Gets a collection containing the number of episodes.
        /// </summary>
        public virtual ICollection<Episode> Episodes { get; private set; }
    }
}
