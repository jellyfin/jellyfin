namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing the type of view for a library or collection.
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// Shows banners.
        /// </summary>
        Banner = 0,

        /// <summary>
        /// Shows a list of content.
        /// </summary>
        List = 1,

        /// <summary>
        /// Shows poster artwork.
        /// </summary>
        Poster = 2,

        /// <summary>
        /// Shows poster artwork with a card containing the name and year.
        /// </summary>
        PosterCard = 3,

        /// <summary>
        /// Shows a thumbnail.
        /// </summary>
        Thumb = 4,

        /// <summary>
        /// Shows a thumbnail with a card containing the name and year.
        /// </summary>
        ThumbCard = 5
    }
}
