namespace Jellyfin.Data.Enums
{
    public enum IndexingKind
    {
        /// <summary>
        /// Index by the premiere date.
        /// </summary>
        PremiereDate,

        /// <summary>
        /// Index by the production year.
        /// </summary>
        ProductionYear,

        /// <summary>
        /// Index by the community rating.
        /// </summary>
        CommunityRating
    }
}
