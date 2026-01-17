#nullable disable

#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public LinkedChild()
        {
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        [Obsolete("Use ItemId instead")]
        public string Path { get; set; }

        public LinkedChildType Type { get; set; }

        /// <summary>
        /// Gets or sets the library item id.
        /// </summary>
        [Obsolete("Use ItemId instead")]
        public string LibraryItemId { get; set; }

        /// <summary>
        /// Gets or sets the linked item id.
        /// </summary>
        public Guid? ItemId { get; set; }

        public static LinkedChild Create(BaseItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new LinkedChild
            {
                ItemId = item.Id,
                Type = LinkedChildType.Manual
            };
        }
    }
}
