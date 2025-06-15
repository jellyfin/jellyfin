namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// Defines all possible methods for database write access.
/// </summary>
public enum DatabaseWriteBehaviorTypes
{
    /// <summary>
    /// Defines that provider concurrent database write access should be relied on.
    /// </summary>
    ConcurrentWrites = 0,

    /// <summary>
    /// Defines that provider serial database write access should be relied on.
    /// </summary>
    SerializedWrites = 1
}
