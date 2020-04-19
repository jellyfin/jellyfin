using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// The base Sqlite repository class.
    /// </summary>
    public abstract class BaseSqliteRepository : IDisposable
    {
        private bool _disposed;

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

        /// <summary>
        /// Gets a connection to the database.
        /// </summary>
        /// <returns>A <see cref="ManagedConnection"/>.</returns>
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

        /// <summary>
        /// Prepares the provided statement.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The resulting <see cref="IStatement"/>.</returns>
        public IStatement PrepareStatement(ManagedConnection connection, string sql)
            => connection.PrepareStatement(sql);

        /// <summary>
        /// Prepares the provided statement.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The resulting <see cref="IStatement"/>.</returns>
        public IStatement PrepareStatement(IDatabaseConnection connection, string sql)
            => connection.PrepareStatement(sql);

        /// <summary>
        /// Prepares the given SQL statements and returns the result.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="sql">The SQL statements.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results.</returns>
        public IEnumerable<IStatement> PrepareAll(IDatabaseConnection connection, IEnumerable<string> sql)
            => sql.Select(connection.PrepareStatement);

        /// <summary>
        /// Checks whether a table with the given name exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="name">The table name to check for.</param>
        /// <returns>Whether a table with the given name exists.</returns>
        protected bool TableExists(ManagedConnection connection, string name)
        {
            return connection.RunInTransaction(db =>
            {
                using var statement = PrepareStatement(db, "select DISTINCT tbl_name from sqlite_master");

                return statement.ExecuteQuery()
                    .Any(row => string.Equals(name, row.GetString(0), StringComparison.OrdinalIgnoreCase));
            }, ReadTransactionMode);
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> containing the column names for a table.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="table">The name of the table to get column names for.</param>
        /// <returns>A <see cref="List{T}"/> containing the column names.</returns>
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

        /// <summary>
        /// Adds a column to a table.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="table">The table to add the column to.</param>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="type">The data type for the new column.</param>
        /// <param name="existingColumnNames">The existing column names. If <paramref name="columnName"/> already exists in this list, then this method does nothing.</param>
        protected void AddColumn(IDatabaseConnection connection, string table, string columnName, string type, List<string> existingColumnNames)
        {
            if (existingColumnNames.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            connection.Execute("alter table " + table + " add column " + columnName + " " + type + " NULL");
        }

        /// <summary>
        /// Ensures that this object has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If this object has been disposed.</exception>
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
