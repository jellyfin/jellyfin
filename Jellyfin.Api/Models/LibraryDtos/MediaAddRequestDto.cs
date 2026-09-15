using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Request for directly adding media paths to the library.
/// </summary>
public class MediaAddRequestDto
{
    /// <summary>
    /// Gets or sets the media paths to add.
    /// </summary>
    public IReadOnlyList<string> Paths { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether existing items should be refreshed.
    /// </summary>
    public bool RefreshExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether newly created items should be refreshed.
    /// </summary>
    public bool RefreshNewItems { get; set; } = true;
}
