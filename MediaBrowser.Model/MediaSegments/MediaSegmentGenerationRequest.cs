using System;

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
}
