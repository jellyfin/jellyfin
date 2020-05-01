#pragma warning disable CS1591

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum MetadataProviders
    /// </summary>
    public enum MetadataProviders
    {
        /// <summary>
        /// The imdb
        /// </summary>
        Imdb = 2,
        /// <summary>
        /// The TMDB
        /// </summary>
        Tmdb = 3,
        /// <summary>
        /// The TVDB
        /// </summary>
        Tvdb = 4,
        /// <summary>
        /// The tvcom
        /// </summary>
        Tvcom = 5,
        /// <summary>
        /// Tmdb Collection Id
        /// </summary>
        TmdbCollection = 7,
        MusicBrainzAlbum = 8,
        MusicBrainzAlbumArtist = 9,
        MusicBrainzArtist = 10,
        MusicBrainzReleaseGroup = 11,
        Zap2It = 12,
        TvRage = 15,
        AudioDbArtist = 16,
        AudioDbAlbum = 17,
        MusicBrainzTrack = 18,
        TvMaze = 19
    }
}
