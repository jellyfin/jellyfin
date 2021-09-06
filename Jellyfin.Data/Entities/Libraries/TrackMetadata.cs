namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity holding metadata for a track.
    /// </summary>
    public class TrackMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public TrackMetadata(string title, string language) : base(title, language)
        {
        }
    }
}
