#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Extensions;
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
        protected BaseSqliteRepository(ILogger<BaseSqliteRepository> logger)
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
        protected ILogger<BaseSqliteRepository> Logger { get; }

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
        /// Gets the journal mode. <see href="https://www.sqlite.org/pragma.html#pragma_journal_mode" />.
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

        protected ManagedConnection GetConnection(bool readOnly = false)
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

        public IStatement[] PrepareAll(IDatabaseConnection connection, IReadOnlyList<string> sql)
        {
            int len = sql.Count;
            IStatement[] statements = new IStatement[len];
            for (int i = 0; i < len; i++)
            {
                statements[i] = connection.PrepareStatement(sql[i]);
            }

            return statements;
        }

        protected bool TableExists(ManagedConnection connection, string name)
        {
            return connection.RunInTransaction(
                db =>
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
                },
                ReadTransactionMode);
        }

        protected List<string> GetColumnNames(IDatabaseConnection connection, string table)
        {
            var columnNames = new List<string>();

            foreach (var row in connection.Query("PRAGMA table_info(" + table + ")"))
            {
                if (row.TryGetString(1, out var columnName))
                {
                    columnNames.Add(columnName);
                }
            }

            return columnNames;
        }

        protected void AddColumn(IDatabaseConnection connection, string table, string columnName, string type, List<string> existingColumnNames)
        {
            if (existingColumnNames.Contains(columnName, StringComparison.OrdinalIgnoreCase))
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
}
