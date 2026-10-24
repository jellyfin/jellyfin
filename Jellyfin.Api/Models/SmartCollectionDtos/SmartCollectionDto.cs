using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions.Json.Converters;

namespace Jellyfin.Api.Models.SmartCollectionDtos;

/// <summary>
/// Smart collection dto.
/// </summary>
public class SmartCollectionDto
{
    /// <summary>
    /// Gets or sets the smart collection id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the smart collection name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the owner user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the filters used to build the smart collection.
    /// </summary>
    public required JsonElement Filters { get; set; }

    /// <summary>
    /// Gets or sets specify one or more sort fields, comma delimited.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<ItemSortBy>? SortBy { get; set; }

    /// <summary>
    /// Gets or sets sort order.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<SortOrder>? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items returned by the smart collection.
    /// </summary>
    public int? Limit { get; set; }
}
