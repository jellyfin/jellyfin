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

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
        }

        protected string DbFilePath { get; set; }

        protected ILogger Logger { get; }

        protected virtual ConnectionFlags DefaultConnectionFlags => ConnectionFlags.NoMutex;

        protected TransactionMode TransactionMode => TransactionMode.Deferred;

        protected TransactionMode ReadTransactionMode => TransactionMode.Deferred;

        protected virtual int? CacheSize => null;

        protected virtual string JournalMode => "WAL";

        protected virtual int? PageSize => null;

        protected virtual TempStoreMode TempStore => TempStoreMode.Default;

        protected virtual SynchronousMode? Synchronous => null;

        protected SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);

        protected SQLiteDatabaseConnection WriteConnection;

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
                WriteConnection.Execute("PRAGMA cache_size=" + (int)CacheSize.Value);
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
                WriteConnection.Execute("PRAGMA page_size=" + (int)PageSize.Value);
            }

            WriteConnection.Execute("PRAGMA temp_store=" + (int)TempStore);

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
            var list = new List<string>();

            foreach (var row in connection.Query("PRAGMA table_info(" + table + ")"))
            {
                if (row[1].SQLiteType != SQLiteType.Null)
                {
                    var name = row[1].ToString();

                    list.Add(name);
                }
            }

            return list;
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
                    WriteConnection.Dispose();
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

    public enum SynchronousMode
    {
        Off = 0,
        Normal = 1,
        Full = 2,
        Extra = 3
    }

    public enum TempStoreMode
    {
        Default = 0,
        File = 1,
        Memory = 2
    }
}
