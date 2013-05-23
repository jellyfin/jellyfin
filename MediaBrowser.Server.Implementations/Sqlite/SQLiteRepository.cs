using MediaBrowser.Model.Logging;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SqliteRepository
    /// </summary>
    public abstract class SqliteRepository : IDisposable
    {
        /// <summary>
        /// The db file name
        /// </summary>
        protected string DbFileName;
        /// <summary>
        /// The connection
        /// </summary>
        protected SQLiteConnection Connection;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteRepository" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        protected SqliteRepository(ILogManager logManager)
        {
            if (logManager == null)
            {
                throw new ArgumentNullException("logManager");
            }

            Logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Connects to DB.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        protected async Task ConnectToDb(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            DbFileName = dbPath;
            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 40960,
                SyncMode = SynchronizationModes.Off,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Memory
            };

            Connection = new SQLiteConnection(connectionstr.ConnectionString);

            await Connection.OpenAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Runs the queries.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException">queries</exception>
        protected void RunQueries(string[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }

            using (var tran = Connection.BeginTransaction())
            {
                try
                {
                    using (var cmd = Connection.CreateCommand())
                    {
                        foreach (var query in queries)
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = query;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch (Exception e)
                {
                    Logger.ErrorException("Error running queries", e);
                    tran.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
                    {
                        if (Connection != null)
                        {
                            if (Connection.IsOpen())
                            {
                                Connection.Close();
                            }

                            Connection.Dispose();
                            Connection = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error disposing database", ex);
                }
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">cmd</exception>
        public async Task ExecuteCommand(DbCommand cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            using (var tran = Connection.BeginTransaction())
            {
                try
                {
                    cmd.Connection = Connection;
                    cmd.Transaction = tran;

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                    tran.Commit();
                }
                catch (Exception e)
                {
                    Logger.ErrorException("Failed to commit transaction.", e);
                    tran.Rollback();
                }
            }
        }

        /// <summary>
        /// Gets a stream from a DataReader at a given ordinal
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>Stream.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        protected static Stream GetStream(IDataReader reader, int ordinal)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            
            var memoryStream = new MemoryStream();
            var num = 0L;
            var array = new byte[4096];
            long bytes;
            do
            {
                bytes = reader.GetBytes(ordinal, num, array, 0, array.Length);
                memoryStream.Write(array, 0, (int)bytes);
                num += bytes;
            }
            while (bytes > 0L);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
