using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;
using System.Linq;
using SQLitePCL;

namespace Emby.Server.Implementations.Data
{
    public abstract class BaseSqliteRepository : IDisposable
    {
        protected string DbFilePath { get; set; }
        protected ReaderWriterLockSlim WriteLock;

        protected ILogger Logger { get; private set; }

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;

            WriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        protected TransactionMode TransactionMode
        {
            get { return TransactionMode.Deferred; }
        }

        protected TransactionMode ReadTransactionMode
        {
            get { return TransactionMode.Deferred; }
        }

        internal static int ThreadSafeMode { get; set; }

        static BaseSqliteRepository()
        {
            SQLite3.EnableSharedCache = false;

            int rc = raw.sqlite3_config(raw.SQLITE_CONFIG_MEMSTATUS, 0);
            //CheckOk(rc);

            rc = raw.sqlite3_config(raw.SQLITE_CONFIG_MULTITHREAD, 1);
            //rc = raw.sqlite3_config(raw.SQLITE_CONFIG_SINGLETHREAD, 1);
            //rc = raw.sqlite3_config(raw.SQLITE_CONFIG_SERIALIZED, 1);
            //CheckOk(rc);

            rc = raw.sqlite3_enable_shared_cache(1);

            ThreadSafeMode = raw.sqlite3_threadsafe();
        }

        private static bool _versionLogged;

        private string _defaultWal;
        protected ManagedConnection _connection;

        protected virtual bool EnableSingleConnection
        {
            get { return true; }
        }

        protected ManagedConnection CreateConnection(bool isReadOnly = false)
        {
            if (_connection != null)
            {
                return _connection;
            }

            lock (WriteLock)
            {
                if (!_versionLogged)
                {
                    _versionLogged = true;
                    Logger.LogInformation("Sqlite version: " + SQLite3.Version);
                    Logger.LogInformation("Sqlite compiler options: " + string.Join(",", SQLite3.CompilerOptions.ToArray()));
                }

                ConnectionFlags connectionFlags;

                if (isReadOnly)
                {
                    //Logger.LogInformation("Opening read connection");
                    //connectionFlags = ConnectionFlags.ReadOnly;
                    connectionFlags = ConnectionFlags.Create;
                    connectionFlags |= ConnectionFlags.ReadWrite;
                }
                else
                {
                    //Logger.LogInformation("Opening write connection");
                    connectionFlags = ConnectionFlags.Create;
                    connectionFlags |= ConnectionFlags.ReadWrite;
                }

                if (EnableSingleConnection)
                {
                    connectionFlags |= ConnectionFlags.PrivateCache;
                }
                else
                {
                    connectionFlags |= ConnectionFlags.SharedCached;
                }

                connectionFlags |= ConnectionFlags.NoMutex;

                var db = SQLite3.Open(DbFilePath, connectionFlags, null);

                try
                {
                    if (string.IsNullOrWhiteSpace(_defaultWal))
                    {
                        _defaultWal = db.Query("PRAGMA journal_mode").SelectScalarString().First();

                        Logger.LogInformation("Default journal_mode for {0} is {1}", DbFilePath, _defaultWal);
                    }

                    var queries = new List<string>
                    {
                        //"PRAGMA cache size=-10000"
                        //"PRAGMA read_uncommitted = true",
                        "PRAGMA synchronous=Normal"
                    };

                    if (CacheSize.HasValue)
                    {
                        queries.Add("PRAGMA cache_size=" + CacheSize.Value.ToString(CultureInfo.InvariantCulture));
                    }

                    if (EnableTempStoreMemory)
                    {
                        queries.Add("PRAGMA temp_store = memory");
                    }
                    else
                    {
                        queries.Add("PRAGMA temp_store = file");
                    }

                    foreach (var query in queries)
                    {
                        db.Execute(query);
                    }
                }
                catch
                {
                    using (db)
                    {

                    }

                    throw;
                }

                _connection = new ManagedConnection(db, false);

                return _connection;
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

            db.ExecuteAll(string.Join(";", queries.ToArray()));
            Logger.LogInformation("PRAGMA synchronous=" + db.Query("PRAGMA synchronous").SelectScalarString().First());
        }

        protected virtual bool EnableTempStoreMemory
        {
            get
            {
                return false;
            }
        }

        protected virtual int? CacheSize
        {
            get
            {
                return null;
            }
        }

        internal static void CheckOk(int rc)
        {
            string msg = "";

            if (raw.SQLITE_OK != rc)
            {
                throw CreateException((ErrorCode)rc, msg);
            }
        }

        internal static Exception CreateException(ErrorCode rc, string msg)
        {
            var exp = new Exception(msg);

            return exp;
        }

        private bool _disposed;
        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name + " has been disposed and cannot be accessed.");
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Dispose(true);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeConnection();
            }
        }

        private void DisposeConnection()
        {
            try
            {
                lock (_disposeLock)
                {
                    using (WriteLock.Write())
                    {
                        if (_connection != null)
                        {
                            using (_connection)
                            {
                                _connection.Close();
                            }
                            _connection = null;
                        }

                        CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing database");
            }
        }

        protected virtual void CloseConnection()
        {

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

    public static class ReaderWriterLockSlimExtensions
    {
        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterReadLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitReadLock();
                    _sync = null;
                }
            }
        }
        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterWriteLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitWriteLock();
                    _sync = null;
                }
            }
        }

        public class DummyToken : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            //if (BaseSqliteRepository.ThreadSafeMode > 0)
            //{
            //    return new DummyToken();
            //}
            return new WriteLockToken(obj);
        }
        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            //if (BaseSqliteRepository.ThreadSafeMode > 0)
            //{
            //    return new DummyToken();
            //}
            return new WriteLockToken(obj);
        }
    }
}
