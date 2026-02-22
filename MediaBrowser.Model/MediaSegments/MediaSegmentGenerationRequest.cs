using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.MediaSegments;

namespace MediaBrowser.Model;

/// <summary>
/// Model containing the arguments for enumerating the requested media item.
/// </summary>
public record MediaSegmentGenerationRequest
{
    /// <summary>
    /// Gets the Id to the BaseItem the segments should be extracted from.
    /// </summary>
    public Guid ItemId { get; init; }

    /// <summary>
    /// Gets existing media segments generated on an earlier scan by this provider.
    /// </summary>
    public required IReadOnlyList<MediaSegmentDto> ExistingSegments { get; init; }
}
