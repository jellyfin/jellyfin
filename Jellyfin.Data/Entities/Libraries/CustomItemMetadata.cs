using System;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity containing metadata for a custom item.
    /// </summary>
    public class CustomItemMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomItemMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="item">The item.</param>
        public CustomItemMetadata(string title, string language, CustomItem item) : base(title, language)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item.CustomItemMetadata.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomItemMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected CustomItemMetadata()
        {
        }
    }
}
