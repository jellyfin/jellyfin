using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Query options for similar items requests.
/// </summary>
public class SimilarItemsQuery
{
    /// <summary>
    /// Gets or sets the user context.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the DTO options.
    /// </summary>
    public DtoOptions? DtoOptions { get; set; }

    /// <summary>
    /// Gets or sets the item IDs to exclude from results.
    /// </summary>
    public IReadOnlyList<Guid> ExcludeItemIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the artist IDs to exclude from results.
    /// </summary>
    public IReadOnlyList<Guid> ExcludeArtistIds { get; set; } = [];
}
