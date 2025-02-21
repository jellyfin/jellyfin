using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Api.Models.LiveTvDtos;

/// <summary>
/// Get programs dto.
/// </summary>
public class GetProgramsDto
{
    /// <summary>
    /// Gets or sets the channels to return guide information for.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<Guid>? ChannelIds { get; set; }

    /// <summary>
    /// Gets or sets optional. Filter by user id.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the minimum premiere start date.
    /// </summary>
    public DateTime? MinStartDate { get; set; }

    /// <summary>
    /// Gets or sets filter by programs that have completed airing, or not.
    /// </summary>
    public bool? HasAired { get; set; }

    /// <summary>
    /// Gets or sets filter by programs that are currently airing, or not.
    /// </summary>
    public bool? IsAiring { get; set; }

    /// <summary>
    /// Gets or sets the maximum premiere start date.
    /// </summary>
    public DateTime? MaxStartDate { get; set; }

    /// <summary>
    /// Gets or sets the minimum premiere end date.
    /// </summary>
    public DateTime? MinEndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum premiere end date.
    /// </summary>
    public DateTime? MaxEndDate { get; set; }

    /// <summary>
    /// Gets or sets filter for movies.
    /// </summary>
    public bool? IsMovie { get; set; }

    /// <summary>
    /// Gets or sets filter for series.
    /// </summary>
    public bool? IsSeries { get; set; }

    /// <summary>
    /// Gets or sets filter for news.
    /// </summary>
    public bool? IsNews { get; set; }

    /// <summary>
    /// Gets or sets filter for kids.
    /// </summary>
    public bool? IsKids { get; set; }

    /// <summary>
    /// Gets or sets filter for sports.
    /// </summary>
    public bool? IsSports { get; set; }

    /// <summary>
    /// Gets or sets the record index to start at. All items with a lower index will be dropped from the results.
    /// </summary>
    public int? StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets specify one or more sort orders, comma delimited. Options: Name, StartDate.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<ItemSortBy>? SortBy { get; set; }

    /// <summary>
    /// Gets or sets sort order.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<SortOrder>? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the genres to return guide information for.
    /// </summary>
    [JsonConverter(typeof(JsonPipeDelimitedCollectionConverterFactory))]
    public IReadOnlyList<string>? Genres { get; set; }

    /// <summary>
    /// Gets or sets the genre ids to return guide information for.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<Guid>? GenreIds { get; set; }

    /// <summary>
    /// Gets or sets include image information in output.
    /// </summary>
    public bool? EnableImages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retrieve total record count.
    /// </summary>
    [DefaultValue(true)]
    public bool EnableTotalRecordCount { get; set; } = true;

    /// <summary>
    /// Gets or sets the max number of images to return, per image type.
    /// </summary>
    public int? ImageTypeLimit { get; set; }

    /// <summary>
    /// Gets or sets the image types to include in the output.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<ImageType>? EnableImageTypes { get; set; }

    /// <summary>
    /// Gets or sets include user data.
    /// </summary>
    public bool? EnableUserData { get; set; }

    /// <summary>
    /// Gets or sets filter by series timer id.
    /// </summary>
    public string? SeriesTimerId { get; set; }

    /// <summary>
    /// Gets or sets filter by library series id.
    /// </summary>
    public Guid? LibrarySeriesId { get; set; }

    /// <summary>
    /// Gets or sets specify additional fields of information to return in the output.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<ItemFields>? Fields { get; set; }
}
