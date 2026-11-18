namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// The type of recommendation reason for an item.
    /// </summary>
    public enum RecommendationType
    {
        /// <summary>
        /// The item is similar to a recently played item.
        /// </summary>
        SimilarToRecentlyPlayed = 0,

        /// <summary>
        /// The item is similar to a liked item.
        /// </summary>
        SimilarToLikedItem = 1,

        /// <summary>
        /// The item has a director in common with a recently played item.
        /// </summary>
        HasDirectorFromRecentlyPlayed = 2,

        /// <summary>
        /// The item has an actor in common with a recently played item.
        /// </summary>
        HasActorFromRecentlyPlayed = 3,

        /// <summary>
        /// The item has a director that the user liked.
        /// </summary>
        HasLikedDirector = 4,

        /// <summary>
        /// The item has an actor that the user liked.
        /// </summary>
        HasLikedActor = 5
    }
}
