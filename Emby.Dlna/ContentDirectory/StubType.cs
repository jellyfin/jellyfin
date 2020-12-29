#pragma warning disable CS1591
#pragma warning disable SA1602

namespace Emby.Dlna.ContentDirectory
{
    /// <summary>
    /// Defines the DLNA item types.
    /// </summary>
    public enum StubType
    {
        Folder = 0,
        Latest = 2,
        Playlists = 3,
        Albums = 4,
        AlbumArtists = 5,
        Artists = 6,
        Songs = 7,
        Genres = 8,
        FavoriteSongs = 9,
        FavoriteArtists = 10,
        FavoriteAlbums = 11,
        ContinueWatching = 12,
        Movies = 13,
        Collections = 14,
        Favorites = 15,
        NextUp = 16,
        Series = 17,
        FavoriteSeries = 18,
        FavoriteEpisodes = 19
    }
}
