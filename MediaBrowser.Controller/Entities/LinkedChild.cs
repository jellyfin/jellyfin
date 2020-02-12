using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public string Path { get; set; }
        public LinkedChildType Type { get; set; }
        public string LibraryItemId { get; set; }

        [JsonIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Serves as a cache
        /// </summary>
        public Guid? ItemId { get; set; }

        public static LinkedChild Create(BaseItem item)
        {
            var child = new LinkedChild
            {
                Path = item.Path,
                Type = LinkedChildType.Manual
            };

            if (string.IsNullOrEmpty(child.Path))
            {
                child.LibraryItemId = item.Id.ToString("N", CultureInfo.InvariantCulture);
            }

            return child;
        }

        public LinkedChild()
        {
            Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }
    }

    public enum LinkedChildType
    {
        Manual = 0,
        Shortcut = 1
    }

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
            return ((obj.Path ?? string.Empty) + (obj.LibraryItemId ?? string.Empty) + obj.Type).GetHashCode();
        }
    }
}
