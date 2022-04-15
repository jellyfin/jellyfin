namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum MetadataProviders.
    /// </summary>
    public enum MetadataProvider
    {
        /// <summary>
        /// This metadata provider is for users and/or plugins to override the
        /// default merging behaviour.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// The IMDb id.
        /// </summary>
        Imdb = 2,

        /// <summary>
        /// The TMDb id.
        /// </summary>
        Tmdb = 3,

        /// <summary>
        /// The TVDb id.
        /// </summary>
        Tvdb = 4,

        /// <summary>
        /// The tvcom id.
        /// </summary>
        Tvcom = 5,

        /// <summary>
        /// TMDb collection id.
        /// </summary>
        TmdbCollection = 7,

        /// <summary>
        /// The MusicBrainz album id.
        /// </summary>
        MusicBrainzAlbum = 8,

        /// <summary>
        /// The MusicBrainz album artist id.
        /// </summary>
        MusicBrainzAlbumArtist = 9,

        /// <summary>
        /// The MusicBrainz artist id.
        /// </summary>
        MusicBrainzArtist = 10,

        /// <summary>
        /// The MusicBrainz release group id.
        /// </summary>
        MusicBrainzReleaseGroup = 11,

        /// <summary>
        /// The Zap2It id.
        /// </summary>
        Zap2It = 12,

        /// <summary>
        /// The TvRage id.
        /// </summary>
        TvRage = 15,

        /// <summary>
        /// The AudioDb artist id.
        /// </summary>
        AudioDbArtist = 16,

        /// <summary>
        /// The AudioDb collection id.
        /// </summary>
        AudioDbAlbum = 17,

        /// <summary>
        /// The MusicBrainz track id.
        /// </summary>
        MusicBrainzTrack = 18,

        /// <summary>
        /// The TvMaze id.
        /// </summary>
        TvMaze = 19
    }
}
