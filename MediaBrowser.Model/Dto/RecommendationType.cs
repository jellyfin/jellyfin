#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dto
{
    public enum RecommendationType
    {
        SimilarToRecentlyPlayed = 0,

        SimilarToLikedItem = 1,

        HasDirectorFromRecentlyPlayed = 2,

        HasActorFromRecentlyPlayed = 3,

        HasLikedDirector = 4,

        HasLikedActor = 5
    }
}
