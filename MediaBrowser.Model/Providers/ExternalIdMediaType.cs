namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// The specific media type of an <see cref="IExternalId"/>.
    /// </summary>
    /// <remarks>
    /// This is used as a translation key for clients.
    /// </remarks>
    public enum ExternalIdMediaType
    {
        /// <summary>
        /// There is no specific media type associated with the external id, or the external provider only has one
        /// id type so there is no need to be specific.
        /// </summary>
        General,

        /// <summary>
        /// A music album.
        /// </summary>
        Album,

        /// <summary>
        /// The artist of a music album.
        /// </summary>
        AlbumArtist,

        /// <summary>
        /// The artist of a media item.
        /// </summary>
        Artist,

        /// <summary>
        /// A boxed set of media.
        /// </summary>
        BoxSet,

        /// <summary>
        /// A series episode.
        /// </summary>
        Episode,

        /// <summary>
        /// A movie.
        /// </summary>
        Movie,

        /// <summary>
        /// An alternative artist apart from the main artist.
        /// </summary>
        OtherArtist,

        /// <summary>
        /// A person.
        /// </summary>
        Person,

        /// <summary>
        /// A release group.
        /// </summary>
        ReleaseGroup,

        /// <summary>
        /// A single season of a series.
        /// </summary>
        Season,

        /// <summary>
        /// A series.
        /// </summary>
        Series,

        /// <summary>
        /// A music track.
        /// </summary>
        Track
    }
}
