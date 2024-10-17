namespace Jellyfin.Data.Entities;

/// <summary>
/// Provides the Value types for an <see cref="ItemValue"/>.
/// </summary>
#pragma warning disable CA1027 // Mark enums with FlagsAttribute
public enum ItemValueType
#pragma warning restore CA1027 // Mark enums with FlagsAttribute
{
    /// <summary>
    /// Artists.
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value. Cannot apply here.
    Artist = 0,
#pragma warning restore CA1008 // Enums should have zero value

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

    /// <summary>
    /// InheritedTags.
    /// </summary>
    InheritedTags = 6,
}
