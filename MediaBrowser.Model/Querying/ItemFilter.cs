
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Enum ItemFilter
    /// </summary>
    public enum ItemFilter
    {
        /// <summary>
        /// The item is a folder
        /// </summary>
        IsFolder = 1,
        /// <summary>
        /// The item is not folder
        /// </summary>
        IsNotFolder = 2,
        /// <summary>
        /// The item is unplayed
        /// </summary>
        IsUnplayed = 3,
        /// <summary>
        /// The item is played
        /// </summary>
        IsPlayed = 4,
        /// <summary>
        /// The item is a favorite
        /// </summary>
        IsFavorite = 5,
        /// <summary>
        /// The item is recently added
        /// </summary>
        IsRecentlyAdded = 6,
        /// <summary>
        /// The item is resumable
        /// </summary>
        IsResumable = 7
    }
}
