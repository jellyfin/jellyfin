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
            if (x.Type == y.Type)
            {
                return _fileSystem.AreEqual(x.Path, y.Path);
            }

            return false;
        }

        public int GetHashCode(LinkedChild obj)
        {
            return ((obj.Path ?? string.Empty) + (obj.LibraryItemId ?? string.Empty) + obj.Type).GetHashCode(StringComparison.Ordinal);
        }
    }
}
