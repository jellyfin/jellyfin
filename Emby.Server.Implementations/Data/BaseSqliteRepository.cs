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
        protected string DbFilePath { get; set; }

        protected ILogger Logger { get; }

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
        }

        protected TransactionMode TransactionMode => TransactionMode.Deferred;

        protected TransactionMode ReadTransactionMode => TransactionMode.Deferred;

        protected virtual ConnectionFlags DefaultConnectionFlags => ConnectionFlags.NoMutex;

        protected SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);

        protected SQLiteDatabaseConnection WriteConnection;

        private string _defaultWal;

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


            if (string.IsNullOrWhiteSpace(_defaultWal))
            {
                _defaultWal = WriteConnection.Query("PRAGMA journal_mode").SelectScalarString().First();

                Logger.LogInformation("Default journal_mode for {0} is {1}", DbFilePath, _defaultWal);
            }

            if (EnableTempStoreMemory)
            {
                WriteConnection.Execute("PRAGMA temp_store = memory");
            }
            else
            {
                WriteConnection.Execute("PRAGMA temp_store = file");
            }

            return new ManagedConnection(WriteConnection, WriteLock);
        }

        public IStatement PrepareStatement(ManagedConnection connection, string sql)
            => connection.PrepareStatement(sql);

        public IStatement PrepareStatement(IDatabaseConnection connection, string sql)
            => connection.PrepareStatement(sql);

        public IEnumerable<IStatement> PrepareAllSafe(IDatabaseConnection connection, IEnumerable<string> sql)
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

        protected void RunDefaultInitialization(ManagedConnection db)
        {
            var queries = new List<string>
            {
                "PRAGMA journal_mode=WAL",
                "PRAGMA page_size=4096",
                "PRAGMA synchronous=Normal"
            };

            if (EnableTempStoreMemory)
            {
                queries.AddRange(new List<string>
                {
                    "pragma default_temp_store = memory",
                    "pragma temp_store = memory"
                });
            }
            else
            {
                queries.AddRange(new List<string>
                {
                    "pragma temp_store = file"
                });
            }

            db.ExecuteAll(string.Join(";", queries));
            Logger.LogInformation("PRAGMA synchronous=" + db.Query("PRAGMA synchronous").SelectScalarString().First());
        }

        protected virtual bool EnableTempStoreMemory => true;

        private bool _disposed;
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

        private readonly object _disposeLock = new object();

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
    }
}
