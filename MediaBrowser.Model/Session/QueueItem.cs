#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    public class QueueItem : IEquatable<QueueItem>
    {
        public Guid Id { get; set; }

        public string PlaylistItemId { get; set; }

        public bool Equals(QueueItem other) => other is not null && Id.Equals(other.Id) && string.Equals(PlaylistItemId, other.PlaylistItemId, StringComparison.Ordinal);
    }
}
