namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// The specific media type of an <see cref="ExternalIdInfo"/>.
    /// </summary>
    /// <remarks>
    /// Client applications may use this as a translation key.
    /// </remarks>
    public enum ExternalIdMediaType
    {
        /// <summary>
        /// A music album.
        /// </summary>
        Album = 1,

        /// <summary>
        /// The artist of a music album.
        /// </summary>
        AlbumArtist = 2,

        /// <summary>
        /// The artist of a media item.
        /// </summary>
        Artist = 3,

        /// <summary>
        /// A boxed set of media.
        /// </summary>
        BoxSet = 4,

        /// <summary>
        /// A series episode.
        /// </summary>
        Episode = 5,

        /// <summary>
        /// A movie.
        /// </summary>
        Movie = 6,

        /// <summary>
        /// An alternative artist apart from the main artist.
        /// </summary>
        OtherArtist = 7,

        /// <summary>
        /// A person.
        /// </summary>
        Person = 8,

        /// <summary>
        /// A release group.
        /// </summary>
        ReleaseGroup = 9,

        /// <summary>
        /// A single season of a series.
        /// </summary>
        Season = 10,

        /// <summary>
        /// A series.
        /// </summary>
        Series = 11,

        /// <summary>
        /// A music track.
        /// </summary>
        Track = 12,

        /// <summary>
        /// A book.
        /// </summary>
        Book = 13,

        /// <summary>
        /// A music recording.
        /// </summary>
        Recording = 14
    }
}
