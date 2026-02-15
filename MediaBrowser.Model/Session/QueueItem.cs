#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session;

public record QueueItem
{
    public Guid Id { get; set; }

    public string PlaylistItemId { get; set; }
}
