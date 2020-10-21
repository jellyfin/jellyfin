#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelItemSortField
    {
        /// <summary>
        /// Name field item for sorting
        /// </summary>
        Name = 0,

        /// <summary>
        /// Community rating field item for sorting
        /// </summary>
        CommunityRating = 1,

        /// <summary>
        /// Premiere field item for sorting
        /// </summary>
        PremiereDate = 2,

        /// <summary>
        /// Date created field item for sorting
        /// </summary>
        DateCreated = 3,

        /// <summary>
        /// Runtime field item for sorting
        /// </summary>
        Runtime = 4,

        /// <summary>
        /// Play count field item for sorting
        /// </summary>
        PlayCount = 5,

        /// <summary>
        /// Community play count field item for sorting
        /// </summary>
        CommunityPlayCount = 6
    }
}
