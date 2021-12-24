namespace Emby.Server.Implementations.Data;

/// <summary>
/// Storage mode used by temporary database files.
/// </summary>
public enum TempStoreMode
{
    /// <summary>
    /// The compile-time C preprocessor macro SQLITE_TEMP_STORE
    /// is used to determine where temporary tables and indices are stored.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Temporary tables and indices are stored in a file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Temporary tables and indices are kept in as if they were pure in-memory databases memory.
    /// </summary>
    Memory = 2
}
