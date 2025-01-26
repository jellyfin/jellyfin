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
        public CustomItemMetadata(string title, string language) : base(title, language)
        {
        }
    }
}
