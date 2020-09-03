#pragma warning disable CS1591

namespace Jellyfin.Data.Enums
{
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
