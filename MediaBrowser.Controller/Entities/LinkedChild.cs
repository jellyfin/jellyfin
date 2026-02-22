#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public LinkedChild()
        {
        }

        public string Path { get; set; }

        public LinkedChildType Type { get; set; }

        public string LibraryItemId { get; set; }

        /// <summary>
        /// Gets or sets the linked item id.
        /// </summary>
        public Guid? ItemId { get; set; }

        public static LinkedChild Create(BaseItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

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
