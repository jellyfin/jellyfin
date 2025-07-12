using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// SQLite-specific database configuration options.
/// </summary>
public class SqliteOptions
{
    /// <summary>
    /// Gets or Sets the SQLite journal mode. If null, defaults to "WAL".
    /// </summary>
    public string? JournalMode { get; set; }

    /// <summary>
    /// Gets or Sets the SQLite journal size limit in bytes. If null, defaults to 134217728 (128MB).
    /// </summary>
    public long? JournalSizeLimit { get; set; }

    /// <summary>
    /// Gets or Sets the SQLite cache size. No default - not set if not specified.
    /// </summary>
    public int? CacheSize { get; set; }

    /// <summary>
    /// Gets or Sets the SQLite memory-mapped I/O size. No default - not set if not specified.
    /// </summary>
    public long? MmapSize { get; set; }

    /// <summary>
    /// Gets or Sets whether to enable SQLite connection pooling. If null, defaults to false.
    /// </summary>
    public bool? Pooling { get; set; }
}
