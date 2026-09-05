using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Result of directly adding media paths to the library.
/// </summary>
public class MediaAddResultDto
{
    /// <summary>
    /// Gets or sets the per-path add results.
    /// </summary>
    public IReadOnlyList<MediaAddPathResultDto> Results { get; set; } = Array.Empty<MediaAddPathResultDto>();
}

/// <summary>
/// Result of directly adding one media path to the library.
/// </summary>
public class MediaAddPathResultDto
{
    /// <summary>
    /// Gets or sets the requested path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the existing or created item path.
    /// </summary>
    public string? ItemPath { get; set; }

    /// <summary>
    /// Gets or sets the path that was resolved and inserted.
    /// </summary>
    public string? ResolvedPath { get; set; }

    /// <summary>
    /// Gets or sets the parent item path.
    /// </summary>
    public string? ParentPath { get; set; }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the parent item id.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the result status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a status detail.
    /// </summary>
    public string? Error { get; set; }
}
