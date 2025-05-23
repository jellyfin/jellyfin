namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// Defines all possible methods for locking database access for concurrent queries.
/// </summary>
public enum DatabaseLockingBehaviorTypes
{
    /// <summary>
    /// Defines that no explicit application level locking for reads and writes should be done and only provider specific locking should be relied on.
    /// </summary>
    NoLock = 0,

    /// <summary>
    /// Defines a behavior that always blocks all reads while any one write is done.
    /// </summary>
    Pessimistic = 1,

    /// <summary>
    /// Defines that all writes should be attempted and when fail should be retried.
    /// </summary>
    Optimistic = 2
}
