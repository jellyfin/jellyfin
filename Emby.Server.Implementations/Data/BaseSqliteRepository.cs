using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public abstract class BaseSqliteRepository : IDisposable
    {
        protected string DbFilePath { get; set; }

        protected ILogger Logger { get; private set; }

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
        }

        protected TransactionMode TransactionMode => TransactionMode.Deferred;

        protected TransactionMode ReadTransactionMode => TransactionMode.Deferred;

        internal static int ThreadSafeMode { get; set; }

        protected virtual ConnectionFlags DefaultConnectionFlags => ConnectionFlags.SharedCached | ConnectionFlags.FullMutex;

        private readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);

        private SQLiteDatabaseConnection WriteConnection;

        private readonly BlockingCollection<SQLiteDatabaseConnection> ReadConnectionPool = new BlockingCollection<SQLiteDatabaseConnection>();

        static BaseSqliteRepository()
        {
            ThreadSafeMode = raw.sqlite3_threadsafe();
        }

        private string _defaultWal;

        protected async Task CreateConnections()
        {
            await WriteLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (WriteConnection == null)
                {
                    WriteConnection = SQLite3.Open(
                                                DbFilePath,
                                                DefaultConnectionFlags | ConnectionFlags.Create | ConnectionFlags.ReadWrite,
                                                null);
                }

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
            }
            catch
            {

                throw;
            }
            finally
            {
                WriteLock.Release();
            }

            // Add one reading connection for each thread
            int threads = System.Environment.ProcessorCount;
            for (int i = 0; i <= threads; i++)
            {
                ReadConnectionPool.Add(SQLite3.Open(DbFilePath, DefaultConnectionFlags | ConnectionFlags.ReadOnly, null));
            }
        }

        protected ManagedConnection GetConnection(bool isReadOnly = false)
        {
            if (isReadOnly)
            {
                return new ManagedConnection(ReadConnectionPool.Take(), ReadConnectionPool);
            }
            else
            {
                if (WriteConnection == null)
                {
                    throw new InvalidOperationException("Can't access the write connection at this time.");
                }

                WriteLock.Wait();
                return new ManagedConnection(WriteConnection, WriteLock);
            }
        }

        public IStatement PrepareStatement(ManagedConnection connection, string sql)
        {
            return connection.PrepareStatement(sql);
        }

        public IStatement PrepareStatementSafe(ManagedConnection connection, string sql)
        {
            return connection.PrepareStatement(sql);
        }

        public IStatement PrepareStatement(IDatabaseConnection connection, string sql)
        {
            return connection.PrepareStatement(sql);
        }

        public IStatement PrepareStatementSafe(IDatabaseConnection connection, string sql)
        {
            return connection.PrepareStatement(sql);
        }

        public List<IStatement> PrepareAll(IDatabaseConnection connection, IEnumerable<string> sql)
        {
            return PrepareAllSafe(connection, sql);
        }

        public List<IStatement> PrepareAllSafe(IDatabaseConnection connection, IEnumerable<string> sql)
        {
            return sql.Select(connection.PrepareStatement).ToList();
        }

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
