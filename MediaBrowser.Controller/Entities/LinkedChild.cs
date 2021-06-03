#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public LinkedChild()
        {
            Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        public string Path { get; set; }

        public LinkedChildType Type { get; set; }

        public string LibraryItemId { get; set; }

        [JsonIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the linked item id.
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
    }
}
