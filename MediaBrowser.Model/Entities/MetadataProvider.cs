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
        /// The IMDb provider.
        /// </summary>
        Imdb = 2,

        /// <summary>
        /// The TMDb provider.
        /// </summary>
        Tmdb = 3,

        /// <summary>
        /// The TVDb provider.
        /// </summary>
        Tvdb = 4,

        /// <summary>
        /// The tvcom provider.
        /// </summary>
        Tvcom = 5,

        /// <summary>
        /// TMDb collection provider.
        /// </summary>
        TmdbCollection = 7,

        /// <summary>
        /// The MusicBrainz album provider.
        /// </summary>
        MusicBrainzAlbum = 8,

        /// <summary>
        /// The MusicBrainz album artist provider.
        /// </summary>
        MusicBrainzAlbumArtist = 9,

        /// <summary>
        /// The MusicBrainz artist provider.
        /// </summary>
        MusicBrainzArtist = 10,

        /// <summary>
        /// The MusicBrainz release group provider.
        /// </summary>
        MusicBrainzReleaseGroup = 11,

        /// <summary>
        /// The Zap2It provider.
        /// </summary>
        Zap2It = 12,

        /// <summary>
        /// The TvRage provider.
        /// </summary>
        TvRage = 15,

        /// <summary>
        /// The AudioDb artist provider.
        /// </summary>
        AudioDbArtist = 16,

        /// <summary>
        /// The AudioDb collection provider.
        /// </summary>
        AudioDbAlbum = 17,

        /// <summary>
        /// The MusicBrainz track provider.
        /// </summary>
        MusicBrainzTrack = 18,

        /// <summary>
        /// The TvMaze provider.
        /// </summary>
        TvMaze = 19,

        /// <summary>
        /// The MusicBrainz recording provider.
        /// </summary>
        MusicBrainzRecording = 20,
    }
}
