namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Provides the Value types for an <see cref="ItemValue"/>.
/// </summary>
public enum ItemValueType
{
    /// <summary>
    /// Artists.
    /// </summary>
    Artist = 0,

    /// <summary>
    /// Album.
    /// </summary>
    AlbumArtist = 1,

    /// <summary>
    /// Genre.
    /// </summary>
    Genre = 2,

    /// <summary>
    /// Studios.
    /// </summary>
    Studios = 3,

    /// <summary>
    /// Tags.
    /// </summary>
    Tags = 4,
}
