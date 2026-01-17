#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChildComparer : IEqualityComparer<LinkedChild>
    {
        private readonly IFileSystem _fileSystem;

        public LinkedChildComparer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool Equals(LinkedChild x, LinkedChild y)
        {
            if (x.Type != y.Type)
            {
                return false;
            }

            // Compare by ItemId first (preferred)
            if (x.ItemId.HasValue && y.ItemId.HasValue)
            {
                return x.ItemId.Value.Equals(y.ItemId.Value);
            }

#pragma warning disable CS0618 // Type or member is obsolete - fallback for shortcut/legacy comparison
            // Fall back to Path comparison for shortcuts and legacy data
            return _fileSystem.AreEqual(x.Path, y.Path);
#pragma warning restore CS0618
        }

        public int GetHashCode(LinkedChild obj)
        {
            // Use ItemId for hash if available, otherwise fall back to legacy fields
            if (obj.ItemId.HasValue && !obj.ItemId.Value.Equals(Guid.Empty))
            {
                return HashCode.Combine(obj.ItemId.Value, obj.Type);
            }

#pragma warning disable CS0618 // Type or member is obsolete - fallback for shortcut/legacy hashing
            return ((obj.Path ?? string.Empty) + (obj.LibraryItemId ?? string.Empty) + obj.Type).GetHashCode(StringComparison.Ordinal);
#pragma warning restore CS0618
        }
    }
}
