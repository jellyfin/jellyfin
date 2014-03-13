
using System;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class BrowseRequest
    /// </summary>
    public class BrowseRequest
    {
        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        public string ItemType { get; set; }

        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        public string ItemName { get; set; }
        
        /// <summary>
        /// Gets or sets the context (Movies, Music, Tv, etc)
        /// Applicable to genres, studios and persons only because the context of items and artists can be inferred.
        /// This is optional to supply and clients are free to ignore it.
        /// </summary>
        /// <value>The context.</value>
        public string Context { get; set; }
    }

    public class ItemContext
    {
        public const string Music = "Music";
        public const string Movies = "Movies";
        public const string TvShows = "TvShows";
        public const string Games = "Games";
    }
}
