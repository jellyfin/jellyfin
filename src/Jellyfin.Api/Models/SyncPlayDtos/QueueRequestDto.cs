using System;
using System.Collections.Generic;
using MediaBrowser.Model.SyncPlay;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class QueueRequestDto.
/// </summary>
public class QueueRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueRequestDto"/> class.
    /// </summary>
    public QueueRequestDto()
    {
        ItemIds = Array.Empty<Guid>();
    }

    /// <summary>
    /// Gets or sets the items to enqueue.
    /// </summary>
    /// <value>The items to enqueue.</value>
    public IReadOnlyList<Guid> ItemIds { get; set; }

    /// <summary>
    /// Gets or sets the mode in which to add the new items.
    /// </summary>
    /// <value>The enqueue mode.</value>
    public GroupQueueMode Mode { get; set; }
}
