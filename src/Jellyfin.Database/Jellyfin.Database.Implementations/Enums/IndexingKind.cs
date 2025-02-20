namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing a type of indexing in a user's display preferences.
    /// </summary>
    public enum IndexingKind
    {
        /// <summary>
        /// Index by the premiere date.
        /// </summary>
        PremiereDate = 0,

        /// <summary>
        /// Index by the production year.
        /// </summary>
        ProductionYear = 1,

        /// <summary>
        /// Index by the community rating.
        /// </summary>
        CommunityRating = 2
    }
}
