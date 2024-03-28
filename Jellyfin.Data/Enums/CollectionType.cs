#pragma warning disable SA1300 // The name of a C# element does not begin with an upper-case letter. - disabled due to legacy requirement.
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
    unknown = 0,

    /// <summary>
    /// Movies collection.
    /// </summary>
    movies = 1,

    /// <summary>
    /// Tv shows collection.
    /// </summary>
    tvshows = 2,

    /// <summary>
    /// Music collection.
    /// </summary>
    music = 3,

    /// <summary>
    /// Music videos collection.
    /// </summary>
    musicvideos = 4,

    /// <summary>
    /// Trailers collection.
    /// </summary>
    trailers = 5,

    /// <summary>
    /// Home videos collection.
    /// </summary>
    homevideos = 6,

    /// <summary>
    /// Box sets collection.
    /// </summary>
    boxsets = 7,

    /// <summary>
    /// Books collection.
    /// </summary>
    books = 8,

    /// <summary>
    /// Photos collection.
    /// </summary>
    photos = 9,

    /// <summary>
    /// Live tv collection.
    /// </summary>
    livetv = 10,

    /// <summary>
    /// Playlists collection.
    /// </summary>
    playlists = 11,

    /// <summary>
    /// Folders collection.
    /// </summary>
    folders = 12,

    /// <summary>
    /// Tv show series collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvshowseries = 101,

    /// <summary>
    /// Tv genres collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvgenres = 102,

    /// <summary>
    /// Tv genre collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvgenre = 103,

    /// <summary>
    /// Tv latest collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvlatest = 104,

    /// <summary>
    /// Tv next up collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvnextup = 105,

    /// <summary>
    /// Tv resume collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvresume = 106,

    /// <summary>
    /// Tv favorite series collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvfavoriteseries = 107,

    /// <summary>
    /// Tv favorite episodes collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    tvfavoriteepisodes = 108,

    /// <summary>
    /// Latest movies collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    movielatest = 109,

    /// <summary>
    /// Movies to resume collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    movieresume = 110,

    /// <summary>
    /// Movie movie collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    moviemovies = 111,

    /// <summary>
    /// Movie collections collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    moviecollection = 112,

    /// <summary>
    /// Movie favorites collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    moviefavorites = 113,

    /// <summary>
    /// Movie genres collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    moviegenres = 114,

    /// <summary>
    /// Movie genre collection.
    /// </summary>
    [OpenApiIgnoreEnum]
    moviegenre = 115
}
