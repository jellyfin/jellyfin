namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity that holds metadata for a photo.
    /// </summary>
    public class PhotoMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the photo.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public PhotoMetadata(string title, string language) : base(title, language)
        {
        }
    }
}
