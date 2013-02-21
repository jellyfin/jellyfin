
namespace MediaBrowser.Controller.Sorting {
    /// <summary>
    /// Enum SortOrder
    /// </summary>
    public enum SortOrder {

        /// <summary>
        /// Sort by name
        /// </summary>
        Name,
        /// <summary>
        /// Sort by date added to the library
        /// </summary>
        Date,
        /// <summary>
        /// Sort by community rating
        /// </summary>
        Rating,
        /// <summary>
        /// Sort by runtime
        /// </summary>
        Runtime,
        /// <summary>
        /// Sort by year
        /// </summary>
        Year,
        /// <summary>
        /// Custom sort order added by plugins
        /// </summary>
        Custom
    }
}
