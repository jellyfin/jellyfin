#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public abstract class BaseSqliteRepository : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSqliteRepository"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Gets or sets the path to the DB file.
        /// </summary>
        /// <value>Path to the DB file.</value>
        protected string DbFilePath { get; set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the default connection flags.
        /// </summary>
        /// <value>The default connection flags.</value>
        protected virtual ConnectionFlags DefaultConnectionFlags => ConnectionFlags.NoMutex;

        /// <summary>
        /// Gets the transaction mode.
        /// </summary>
        /// <value>The transaction mode.</value>>
        protected TransactionMode TransactionMode => TransactionMode.Deferred;

        /// <summary>
        /// Gets the transaction mode for read-only operations.
        /// </summary>
        /// <value>The transaction mode.</value>
        protected TransactionMode ReadTransactionMode => TransactionMode.Deferred;

        /// <summary>
        /// Gets the cache size.
        /// </summary>
        /// <value>The cache size or null.</value>
        protected virtual int? CacheSize => null;

        /// <summary>
        /// Gets the journal mode. <see href="https://www.sqlite.org/pragma.html#pragma_journal_mode" />
        /// </summary>
        /// <value>The journal mode.</value>
        protected virtual string JournalMode => "TRUNCATE";

        /// <summary>
        /// Gets the page size.
        /// </summary>
        /// <value>The page size or null.</value>
        protected virtual int? PageSize => null;

        /// <summary>
        /// Gets the temp store mode.
        /// </summary>
        /// <value>The temp store mode.</value>
        /// <see cref="TempStoreMode"/>
        protected virtual TempStoreMode TempStore => TempStoreMode.Default;

        /// <summary>
        /// Gets the synchronous mode.
        /// </summary>
        /// <value>The synchronous mode or null.</value>
        /// <see cref="SynchronousMode"/>
        protected virtual SynchronousMode? Synchronous => null;

        /// <summary>
        /// Gets or sets the write lock.
        /// </summary>
        /// <value>The write lock.</value>
        protected SemaphoreSlim WriteLock { get; set; } = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets or sets the write connection.
        /// </summary>
        /// <value>The write connection.</value>
        protected SQLiteDatabaseConnection WriteConnection { get; set; }

        protected ManagedConnection GetConnection(bool _ = false)
        {
            WriteLock.Wait();
            if (WriteConnection != null)
            {
                return new ManagedConnection(WriteConnection, WriteLock);
            }

            WriteConnection = SQLite3.Open(
                DbFilePath,
                DefaultConnectionFlags | ConnectionFlags.Create | ConnectionFlags.ReadWrite,
                null);

            if (CacheSize.HasValue)
            {
                WriteConnection.Execute("PRAGMA cache_size=" + CacheSize.Value);
            }

            if (!string.IsNullOrWhiteSpace(JournalMode))
            {
                WriteConnection.Execute("PRAGMA journal_mode=" + JournalMode);
            }

            if (Synchronous.HasValue)
            {
                WriteConnection.Execute("PRAGMA synchronous=" + (int)Synchronous.Value);
            }

            if (PageSize.HasValue)
            {
                WriteConnection.Execute("PRAGMA page_size=" + PageSize.Value);
            }

            WriteConnection.Execute("PRAGMA temp_store=" + (int)TempStore);

            // Configuration and pragmas can affect VACUUM so it needs to be last.
            WriteConnection.Execute("VACUUM");

            return new ManagedConnection(WriteConnection, WriteLock);
        }

        public IStatement PrepareStatement(ManagedConnection connection, string sql)
            => connection.PrepareStatement(sql);

        public IStatement PrepareStatement(IDatabaseConnection connection, string sql)
            => connection.PrepareStatement(sql);

        public IEnumerable<IStatement> PrepareAll(IDatabaseConnection connection, IEnumerable<string> sql)
            => sql.Select(connection.PrepareStatement);

        protected bool TableExists(ManagedConnection connection, string name)
        {
            return connection.RunInTransaction(db =>
            {
                using (var statement = PrepareStatement(db, "select DISTINCT tbl_name from sqlite_master"))
                {
                    foreach (var row in statement.ExecuteQuery())
                    {
                        if (string.Equals(name, row.GetString(0), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;

            }, ReadTransactionMode);
        }

        protected List<string> GetColumnNames(IDatabaseConnection connection, string table)
        {
            var columnNames = new List<string>();

            foreach (var row in connection.Query("PRAGMA table_info(" + table + ")"))
            {
                if (row[1].SQLiteType != SQLiteType.Null)
                {
                    var name = row[1].ToString();

                    columnNames.Add(name);
                }
            }

            return columnNames;
        }

        protected void AddColumn(IDatabaseConnection connection, string table, string columnName, string type, List<string> existingColumnNames)
        {
            if (existingColumnNames.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            connection.Execute("alter table " + table + " add column " + columnName + " " + type + " NULL");
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name, "Object has been disposed and cannot be accessed.");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            if (dispose)
            {
                WriteLock.Wait();
                try
                {
                    WriteConnection?.Dispose();
                }
                finally
                {
                    WriteLock.Release();
                }

                WriteLock.Dispose();
            }

            WriteConnection = null;
            WriteLock = null;

            _disposed = true;
        }
    }

    /// <summary>
    /// The disk synchronization mode, controls how aggressively SQLite will write data
    /// all the way out to physical storage.
    /// </summary>
    public enum SynchronousMode
    {
        /// <summary>
        /// SQLite continues without syncing as soon as it has handed data off to the operating system
        /// </summary>
        Off = 0,

        /// <summary>
        /// SQLite database engine will still sync at the most critical moments
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
}
