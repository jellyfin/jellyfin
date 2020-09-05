namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing an unrated item.
    /// </summary>
    public enum UnratedItem
    {
        /// <summary>
        /// A movie.
        /// </summary>
        Movie = 0,

        /// <summary>
        /// A trailer.
        /// </summary>
        Trailer = 1,

        /// <summary>
        /// A series.
        /// </summary>
        Series = 2,

        /// <summary>
        /// Music.
        /// </summary>
        Music = 3,

        /// <summary>
        /// A book.
        /// </summary>
        Book = 4,

        /// <summary>
        /// A live TV channel
        /// </summary>
        LiveTvChannel = 5,

        /// <summary>
        /// A live TV program.
        /// </summary>
        LiveTvProgram = 6,

        /// <summary>
        /// Channel content.
        /// </summary>
        ChannelContent = 7,

        /// <summary>
        /// Another type, not covered by the other fields.
        /// </summary>
        Other = 8
    }
}
