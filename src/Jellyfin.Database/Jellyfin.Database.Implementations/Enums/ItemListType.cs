namespace Jellyfin.Database.Implementations.Enums;

/// <summary>
/// The types of item lists.
/// </summary>
public enum ItemListType
{
    /// <summary>
    /// A collection.
    /// </summary>
    Collection = 0,

    /// <summary>
    /// A playlist.
    /// </summary>
    Playlist = 1,

    /// <summary>
    /// A watchlist.
    /// </summary>
    Watchlist = 2
}
