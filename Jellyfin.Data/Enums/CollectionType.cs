using Jellyfin.Data.Attributes;

namespace Jellyfin.Data.Enums;

/// <summary>
/// Collection type.
/// </summary>
public enum CollectionType
{
    /// <summary>
    /// Unknown collection.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Movies collection.
    /// </summary>
    Movies = 1,

    /// <summary>
    /// Tv shows collection.
    /// </summary>
    TvShows = 2,

    /// <summary>
    /// Music collection.
    /// </summary>
    Music = 3,

    /// <summary>
    /// Music videos collection.
    /// </summary>
    MusicVideos = 4,

    /// <summary>
    /// Trailers collection.
    /// </summary>
    Trailers = 5,

    /// <summary>
    /// Home videos collection.
    /// </summary>
    HomeVideos = 6,

    /// <summary>
    /// Box sets collection.
    /// </summary>
    BoxSets = 7,

    /// <summary>
    /// Books collection.
    /// </summary>
    Books = 8,

    /// <summary>
    /// Photos collection.
    /// </summary>
    Photos = 9,

    /// <summary>
    /// Live tv collection.
    /// </summary>
    LiveTv = 10,

    /// <summary>
    /// Playlists collection.
    /// </summary>
    Playlists = 11,

    /// <summary>
    /// Folders collection.
    /// </summary>
    Folders = 12,

    /// <summary>
    /// Tv show series collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvShowSeries = 101,

    /// <summary>
    /// Tv genres collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvGenres = 102,

    /// <summary>
    /// Tv genre collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvGenre = 103,

    /// <summary>
    /// Tv latest collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvLatest = 104,

    /// <summary>
    /// Tv next up collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvNextUp = 105,

    /// <summary>
    /// Tv resume collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvResume = 106,

    /// <summary>
    /// Tv favorite series collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvFavoriteSeries = 107,

    /// <summary>
    /// Tv favorite episodes collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    TvFavoriteEpisodes = 108,

    /// <summary>
    /// Latest movies collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieLatest = 109,

    /// <summary>
    /// Movies to resume collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieResume = 110,

    /// <summary>
    /// Movie movie collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieMovies = 111,

    /// <summary>
    /// Movie collections collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieCollections = 112,

    /// <summary>
    /// Movie favorites collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieFavorites = 113,

    /// <summary>
    /// Movie genres collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieGenres = 114,

    /// <summary>
    /// Movie genre collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    MovieGenre = 115
}
