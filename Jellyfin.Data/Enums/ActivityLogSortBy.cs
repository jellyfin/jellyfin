namespace Jellyfin.Data.Enums;

/// <summary>
/// Activity log sorting options.
/// </summary>
public enum ActivityLogSortBy
{
    /// <summary>
    /// Sort by name.
    /// </summary>
    Name = 0,

    /// <summary>
    /// Sort by overview.
    /// </summary>
    Overiew = 1,

    /// <summary>
    /// Sort by short overview.
    /// </summary>
    ShortOverview = 2,

    /// <summary>
    /// Sort by type.
    /// </summary>
    Type = 3,

    /// <summary>
    /// Sort by item id.
    /// </summary>
    ItemId = 4,

    /// <summary>
    /// Sort by date.
    /// </summary>
    DateCreated = 5,

    /// <summary>
    /// Sort by user id.
    /// </summary>
    UserId = 6,

    /// <summary>
    /// Sort by severity.
    /// </summary>
    LogSeverity = 7
}
