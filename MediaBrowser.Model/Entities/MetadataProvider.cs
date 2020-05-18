#pragma warning disable CS1591

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum MetadataProvider.
    /// </summary>
    public enum MetadataProvider
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

        /// <summary>
        /// Musicbrainz album
        /// </summary>
        MusicBrainzAlbum = 8,

        /// <summary>
        /// Musicbrainz album artist
        /// </summary>
        MusicBrainzAlbumArtist = 9,

        /// <summary>
        /// Musicbrainz artist
        /// </summary>
        MusicBrainzArtist = 10,

        /// <summary>
        /// Musicbrainz release group
        /// </summary>
        MusicBrainzReleaseGroup = 11,

        /// <summary>
        /// Zap2It
        /// </summary>
        Zap2It = 12,

        /// <summary>
        /// TvRage
        /// </summary>
        TvRage = 15,

        /// <summary>
        /// AudioDb artist
        /// </summary>
        AudioDbArtist = 16,

        /// <summary>
        /// AudioDb album
        /// </summary>
        AudioDbAlbum = 17,

        /// <summary>
        /// Musicbrainz track
        /// </summary>
        MusicBrainzTrack = 18,

        /// <summary>
        /// TV Maze
        /// </summary>
        TvMaze = 19
    }
}
