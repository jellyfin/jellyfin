namespace Emby.Server.Implementations.Data;

/// <summary>
/// The disk synchronization mode, controls how aggressively SQLite will write data
/// all the way out to physical storage.
/// </summary>
public enum SynchronousMode
{
    /// <summary>
    /// SQLite continues without syncing as soon as it has handed data off to the operating system.
    /// </summary>
    Off = 0,

    /// <summary>
    /// SQLite database engine will still sync at the most critical moments.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// SQLite database engine will use the xSync method of the VFS
    /// to ensure that all content is safely written to the disk surface prior to continuing.
    /// </summary>
    Full = 2,

    /// <summary>
    /// EXTRA synchronous is like FULL with the addition that the directory containing a rollback journal
    /// is synced after that journal is unlinked to commit a transaction in DELETE mode.
    /// </summary>
    Extra = 3
}
