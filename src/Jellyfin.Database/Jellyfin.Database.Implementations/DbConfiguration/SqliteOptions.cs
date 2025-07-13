using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// SQLite-specific database configuration options.
/// </summary>
 public class SqliteOptions
{
    /// <summary>
    /// SQLite default journal size limit (unlimited).
    /// </summary>
    public const long DefaultJournalSizeLimit = -1;

    /// <summary>
    /// SQLite default cache size.
    /// </summary>
    public const int DefaultCacheSize = -2000;

    /// <summary>
    /// SQLite default memory-mapped I/O size.
    /// </summary>
    public const long DefaultMmapSize = 0;

    /// <summary>
    /// SQLite default page size.
    /// </summary>
    public const int DefaultPageSize = 4096;

    /// <summary>
    /// SQLite default synchronous setting for WAL mode.
    /// </summary>
    public const string DefaultSynchronous = "NORMAL"; // default for journal_mode = WAL

    /// <summary>
    /// Gets or Sets the database directory path. Defaults to DataPath.
    /// </summary>
    public string DatabaseDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or Sets a value indicating whether to enable SQLite connection pooling. Defaults to false.
    /// </summary>
    public bool Pooling { get; set; } = false;

    /// <summary>
    /// Gets or Sets the SQLite journal mode (PRAGMA journal_mode). Defaults to "WAL".
    /// </summary>
    public string JournalMode { get; set; } = "WAL";

    /// <summary>
    /// Gets or Sets the SQLite journal size limit (PRAGMA journal_size_limit) in bytes. Defaults to 134217728 (128MB).
    /// </summary>
    public long JournalSizeLimit { get; set; } = 128 * 1024 * 1024;

    /// <summary>
    /// Gets or Sets the SQLite cache size (PRAGMA cache_size). Negative values are expressed in kibibytes (1024 bytes), and positive numbers are expressed in SQLite pages.  Defaults to -2000 (2MB).
    /// </summary>
    public int CacheSize { get; set; } = -2000;

    /// <summary>
    /// Gets or Sets the SQLite memory-mapped I/O size (PRAGMA mmap_size) in bytes. Defaults to 0.
    /// </summary>
    public long MmapSize { get; set; } = 0;

    /// <summary>
    /// Gets or Sets the SQLite page size (PRAGMA page_size) in bytes for new databases only. Defaults to 4096.
    /// </summary>
    public int PageSize { get; set; } = 4096;

    /// <summary>
    /// Gets or Sets the SQLite synchronous write behaviour (PRAGMA synchronous). Values are OFF, NORMAL, FULL, EXTRA. Defaults to NORMAL.
    /// </summary>
    public string Synchronous { get; set; } = "NORMAL";
}
