#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelItemSortField
    {
        /// <summary>
        /// Name
        /// </summary>
        Name = 0,

        /// <summary>
        /// Community rating
        /// </summary>
        CommunityRating = 1,

        /// <summary>
        /// Premiere date
        /// </summary>
        PremiereDate = 2,

        /// <summary>
        /// DateCreated
        /// </summary>
        DateCreated = 3,

        /// <summary>
        /// Runtime
        /// </summary>
        Runtime = 4,

        /// <summary>
        /// PlayCount
        /// </summary>
        PlayCount = 5,

        /// <summary>
        /// CommunityPlayCount
        /// </summary>
        CommunityPlayCount = 6
    }
}
