using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.PlaylistDtos;

/// <summary>
/// DTO used to reorder all items in a playlist in a single request.
/// </summary>
public class ReorderPlaylistItemsDto
{
    /// <summary>
    /// Gets or sets the desired item order, expressed as PlaylistItemId strings
    /// (the 32-character hex ItemId returned in <c>PlaylistItemId</c> fields).
    /// The playlist will be rearranged to match this sequence.
    /// Any valid entry IDs omitted from the list are appended after the supplied
    /// entries in their previous relative order.
    /// </summary>
    [Required]
    public IReadOnlyList<string> EntryIds { get; set; } = [];
}
