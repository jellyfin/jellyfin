using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Api.Models.LiveTvDtos
{
    /// <summary>
    /// Get programs dto.
    /// </summary>
    public class GetProgramsDto
    {
        /// <summary>
        /// Gets or sets the channels to return guide information for.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<Guid> ChannelIds { get; set; } = Array.Empty<Guid>();

        /// <summary>
        /// Gets or sets optional. Filter by user id.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the minimum premiere start date.
        /// Optional.
        /// </summary>
        public DateTime? MinStartDate { get; set; }

        /// <summary>
        /// Gets or sets filter by programs that have completed airing, or not.
        /// Optional.
        /// </summary>
        public bool? HasAired { get; set; }

        /// <summary>
        /// Gets or sets filter by programs that are currently airing, or not.
        /// Optional.
        /// </summary>
        public bool? IsAiring { get; set; }

        /// <summary>
        /// Gets or sets the maximum premiere start date.
        /// Optional.
        /// </summary>
        public DateTime? MaxStartDate { get; set; }

        /// <summary>
        /// Gets or sets the minimum premiere end date.
        /// Optional.
        /// </summary>
        public DateTime? MinEndDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum premiere end date.
        /// Optional.
        /// </summary>
        public DateTime? MaxEndDate { get; set; }

        /// <summary>
        /// Gets or sets filter for movies.
        /// Optional.
        /// </summary>
        public bool? IsMovie { get; set; }

        /// <summary>
        /// Gets or sets filter for series.
        /// Optional.
        /// </summary>
        public bool? IsSeries { get; set; }

        /// <summary>
        /// Gets or sets filter for news.
        /// Optional.
        /// </summary>
        public bool? IsNews { get; set; }

        /// <summary>
        /// Gets or sets filter for kids.
        /// Optional.
        /// </summary>
        public bool? IsKids { get; set; }

        /// <summary>
        /// Gets or sets filter for sports.
        /// Optional.
        /// </summary>
        public bool? IsSports { get; set; }

        /// <summary>
        /// Gets or sets the record index to start at. All items with a lower index will be dropped from the results.
        /// Optional.
        /// </summary>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records to return.
        /// Optional.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets specify one or more sort orders, comma delimited. Options: Name, StartDate.
        /// Optional.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<string> SortBy { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets sort Order - Ascending,Descending.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<SortOrder> SortOrder { get; set; } = Array.Empty<SortOrder>();

        /// <summary>
        /// Gets or sets the genres to return guide information for.
        /// </summary>
        [JsonConverter(typeof(JsonPipeDelimitedArrayConverterFactory))]
        public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the genre ids to return guide information for.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<Guid> GenreIds { get; set; } = Array.Empty<Guid>();

        /// <summary>
        /// Gets or sets include image information in output.
        /// Optional.
        /// </summary>
        public bool? EnableImages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether retrieve total record count.
        /// </summary>
        public bool EnableTotalRecordCount { get; set; } = true;

        /// <summary>
        /// Gets or sets the max number of images to return, per image type.
        /// Optional.
        /// </summary>
        public int? ImageTypeLimit { get; set; }

        /// <summary>
        /// Gets or sets the image types to include in the output.
        /// Optional.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<ImageType> EnableImageTypes { get; set; } = Array.Empty<ImageType>();

        /// <summary>
        /// Gets or sets include user data.
        /// Optional.
        /// </summary>
        public bool? EnableUserData { get; set; }

        /// <summary>
        /// Gets or sets filter by series timer id.
        /// Optional.
        /// </summary>
        public string? SeriesTimerId { get; set; }

        /// <summary>
        /// Gets or sets filter by library series id.
        /// Optional.
        /// </summary>
        public Guid LibrarySeriesId { get; set; }

        /// <summary>
        /// Gets or sets specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.
        /// Optional.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<ItemFields> Fields { get; set; } = Array.Empty<ItemFields>();
    }
}
