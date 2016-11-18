using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public abstract class BaseSqliteRepository : IDisposable
    {
        protected string DbFilePath { get; set; }
        protected SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
        protected ILogger Logger { get; private set; }

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
        }

        protected virtual bool EnableConnectionPooling
        {
            get { return true; }
        }

        protected virtual SQLiteDatabaseConnection CreateConnection(bool isReadOnly = false)
        {
            SQLite3.EnableSharedCache = false;

            ConnectionFlags connectionFlags;

            if (isReadOnly)
            {
                connectionFlags = ConnectionFlags.ReadOnly;
                //connectionFlags = ConnectionFlags.Create;
                //connectionFlags |= ConnectionFlags.ReadWrite;
            }
            else
            {
                connectionFlags = ConnectionFlags.Create;
                connectionFlags |= ConnectionFlags.ReadWrite;
            }

            if (EnableConnectionPooling)
            {
                connectionFlags |= ConnectionFlags.SharedCached;
            }
            else
            {
                connectionFlags |= ConnectionFlags.PrivateCache;
            }

            connectionFlags |= ConnectionFlags.NoMutex;

            var db = SQLite3.Open(DbFilePath, connectionFlags, null);

            var queries = new[]
            {
                "PRAGMA page_size=4096",
                "PRAGMA journal_mode=WAL",
                "PRAGMA temp_store=memory",
                "PRAGMA synchronous=Normal",
                //"PRAGMA cache size=-10000"
                };

            //foreach (var query in queries)
            //{
            //    db.Execute(query);
            //}

            db.ExecuteAll(string.Join(";", queries));
            
            return db;
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
                        WriteLock.Wait();

                        CloseConnection();
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

        protected void AddColumn(IDatabaseConnection connection, string table, string columnName, string type)
        {
            foreach (var row in connection.Query("PRAGMA table_info(" + table + ")"))
            {
                if (row[1].SQLiteType != SQLiteType.Null)
                {
                    var name = row[1].ToString();

                    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            connection.ExecuteAll(string.Join(";", new string[]
            {
                "alter table " + table,
                "add column " + columnName + " " + type + " NULL"
            }));
        }
    }
}
