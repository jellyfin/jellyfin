namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing types of art.
    /// </summary>
    public enum ArtKind
    {
        /// <summary>
        /// Another type of art, not covered by the other members.
        /// </summary>
        Other = 0,

        /// <summary>
        /// A poster.
        /// </summary>
        Poster = 1,

        /// <summary>
        /// A banner.
        /// </summary>
        Banner = 2,

        /// <summary>
        /// A thumbnail.
        /// </summary>
        Thumbnail = 3,

        /// <summary>
        /// A logo.
        /// </summary>
        Logo = 4
    }
}
