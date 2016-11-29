using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
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

            WriteLock = AllowLockRecursion ?
              new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion) :
              new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        protected virtual bool AllowLockRecursion
        {
            get { return false; }
        }

        protected TransactionMode TransactionMode
        {
            get { return TransactionMode.Immediate; }
        }

        static BaseSqliteRepository()
        {
            SQLite3.EnableSharedCache = false;

            int rc = raw.sqlite3_config(raw.SQLITE_CONFIG_MEMSTATUS, 0);
            //CheckOk(rc);
        }

        private static bool _versionLogged;

        private string _defaultWal;

        protected SQLiteDatabaseConnection CreateConnection(bool isReadOnly = false)
        {
            if (!_versionLogged)
            {
                _versionLogged = true;
                Logger.Info("Sqlite version: " + SQLite3.Version);
                Logger.Info("Sqlite compiler options: " + string.Join(",", SQLite3.CompilerOptions.ToArray()));
            }

            ConnectionFlags connectionFlags;

            if (isReadOnly)
            {
                //Logger.Info("Opening read connection");
            }
            else
            {
                //Logger.Info("Opening write connection");
            }

            connectionFlags = ConnectionFlags.Create;
            connectionFlags |= ConnectionFlags.ReadWrite;
            connectionFlags |= ConnectionFlags.SharedCached;
            connectionFlags |= ConnectionFlags.NoMutex;

            var db = SQLite3.Open(DbFilePath, connectionFlags, null);

            if (string.IsNullOrWhiteSpace(_defaultWal))
            {
                _defaultWal = db.Query("PRAGMA journal_mode").SelectScalarString().First();

                Logger.Info("Default journal_mode for {0} is {1}", DbFilePath, _defaultWal);
            }

            var queries = new List<string>
            {
                //"PRAGMA cache size=-10000"
            };

            if (EnableTempStoreMemory)
            {
                queries.Add("PRAGMA temp_store = memory");
            }

            //var cacheSize = CacheSize;
            //if (cacheSize.HasValue)
            //{

            //}

            ////foreach (var query in queries)
            ////{
            ////    db.Execute(query);
            ////}

            //Logger.Info("synchronous: " + db.Query("PRAGMA synchronous").SelectScalarString().First());
            //Logger.Info("temp_store: " + db.Query("PRAGMA temp_store").SelectScalarString().First());

            /*if (!string.Equals(_defaultWal, "wal", StringComparison.OrdinalIgnoreCase))
            {
                queries.Add("PRAGMA journal_mode=WAL");

                using (WriteLock.Write())
                {
                    db.ExecuteAll(string.Join(";", queries.ToArray()));
                }
            }
            else*/ if (queries.Count > 0)
            {
                db.ExecuteAll(string.Join(";", queries.ToArray()));
            }

            return db;
        }

        protected void RunDefaultInitialization(IDatabaseConnection db)
        {
            var queries = new List<string>
            {
                "PRAGMA journal_mode=WAL",
                "PRAGMA page_size=4096",
            };

            if (EnableTempStoreMemory)
            {
                queries.AddRange(new List<string>
                {
                    "pragma default_temp_store = memory",
                    "pragma temp_store = memory"
                });
            }

            db.ExecuteAll(string.Join(";", queries.ToArray()));
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
            GC.SuppressFinalize(this);
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
                try
                {
                    lock (_disposeLock)
                    {
                        using (WriteLock.Write())
                        {
                            CloseConnection();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error disposing database", ex);
                }
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

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            return new ReadLockToken(obj);
        }
        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            return new WriteLockToken(obj);
        }
    }
}
