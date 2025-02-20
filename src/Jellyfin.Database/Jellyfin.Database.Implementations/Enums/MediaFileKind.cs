namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing the type of media file.
    /// </summary>
    public enum MediaFileKind
    {
        /// <summary>
        /// The main file.
        /// </summary>
        Main = 0,

        /// <summary>
        /// A sidecar file.
        /// </summary>
        Sidecar = 1,

        /// <summary>
        /// An additional part to the main file.
        /// </summary>
        AdditionalPart = 2,

        /// <summary>
        /// An alternative format to the main file.
        /// </summary>
        AlternativeFormat = 3,

        /// <summary>
        /// An additional stream for the main file.
        /// </summary>
        AdditionalStream = 4
    }
}
