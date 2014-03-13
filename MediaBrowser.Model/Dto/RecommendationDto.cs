
namespace MediaBrowser.Model.Dto
{
    public class RecommendationDto
    {
        public BaseItemDto[] Items { get; set; }

        public RecommendationType RecommendationType { get; set; }

        public string BaselineItemName { get; set; }

        public string CategoryId { get; set; }
    }

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
