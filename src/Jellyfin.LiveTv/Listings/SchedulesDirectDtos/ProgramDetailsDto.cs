using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Program details dto.
    /// </summary>
    public class ProgramDetailsDto
    {
        /// <summary>
        /// Gets or sets the audience.
        /// </summary>
        [JsonPropertyName("audience")]
        public string? Audience { get; set; }

        /// <summary>
        /// Gets or sets the program id.
        /// </summary>
        [JsonPropertyName("programID")]
        public string? ProgramId { get; set; }

        /// <summary>
        /// Gets or sets the list of titles.
        /// </summary>
        [JsonPropertyName("titles")]
        public IReadOnlyList<TitleDto> Titles { get; set; } = Array.Empty<TitleDto>();

        /// <summary>
        /// Gets or sets the event details object.
        /// </summary>
        [JsonPropertyName("eventDetails")]
        public EventDetailsDto? EventDetails { get; set; }

        /// <summary>
        /// Gets or sets the descriptions.
        /// </summary>
        [JsonPropertyName("descriptions")]
        public DescriptionsProgramDto? Descriptions { get; set; }

        /// <summary>
        /// Gets or sets the original air date.
        /// </summary>
        [JsonPropertyName("originalAirDate")]
        public DateTime? OriginalAirDate { get; set; }

        /// <summary>
        /// Gets or sets the list of genres.
        /// </summary>
        [JsonPropertyName("genres")]
        public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        [JsonPropertyName("episodeTitle150")]
        public string? EpisodeTitle150 { get; set; }

        /// <summary>
        /// Gets or sets the list of metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        public IReadOnlyList<MetadataProgramsDto> Metadata { get; set; } = Array.Empty<MetadataProgramsDto>();

        /// <summary>
        /// Gets or sets the list of content ratings.
        /// </summary>
        [JsonPropertyName("contentRating")]
        public IReadOnlyList<ContentRatingDto> ContentRating { get; set; } = Array.Empty<ContentRatingDto>();

        /// <summary>
        /// Gets or sets the list of cast.
        /// </summary>
        [JsonPropertyName("cast")]
        public IReadOnlyList<CastDto> Cast { get; set; } = Array.Empty<CastDto>();

        /// <summary>
        /// Gets or sets the list of crew.
        /// </summary>
        [JsonPropertyName("crew")]
        public IReadOnlyList<CrewDto> Crew { get; set; } = Array.Empty<CrewDto>();

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets the show type.
        /// </summary>
        [JsonPropertyName("showType")]
        public string? ShowType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is image artwork.
        /// </summary>
        [JsonPropertyName("hasImageArtwork")]
        public bool HasImageArtwork { get; set; }

        /// <summary>
        /// Gets or sets the primary image.
        /// </summary>
        [JsonPropertyName("primaryImage")]
        public string? PrimaryImage { get; set; }

        /// <summary>
        /// Gets or sets the thumb image.
        /// </summary>
        [JsonPropertyName("thumbImage")]
        public string? ThumbImage { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image.
        /// </summary>
        [JsonPropertyName("backdropImage")]
        public string? BackdropImage { get; set; }

        /// <summary>
        /// Gets or sets the banner image.
        /// </summary>
        [JsonPropertyName("bannerImage")]
        public string? BannerImage { get; set; }

        /// <summary>
        /// Gets or sets the image id.
        /// </summary>
        [JsonPropertyName("imageID")]
        public string? ImageId { get; set; }

        /// <summary>
        /// Gets or sets the md5.
        /// </summary>
        [JsonPropertyName("md5")]
        public string? Md5 { get; set; }

        /// <summary>
        /// Gets or sets the list of content advisory.
        /// </summary>
        [JsonPropertyName("contentAdvisory")]
        public IReadOnlyList<string> ContentAdvisory { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the movie object.
        /// </summary>
        [JsonPropertyName("movie")]
        public MovieDto? Movie { get; set; }

        /// <summary>
        /// Gets or sets the list of recommendations.
        /// </summary>
        [JsonPropertyName("recommendations")]
        public IReadOnlyList<RecommendationDto> Recommendations { get; set; } = Array.Empty<RecommendationDto>();
    }
}
