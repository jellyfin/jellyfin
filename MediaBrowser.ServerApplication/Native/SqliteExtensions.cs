using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Persistence;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class SQLiteExtensions
    /// </summary>
    static class SqliteExtensions
    {
        /// <summary>
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        public static async Task<IDbConnection> ConnectToDb(string dbPath, ILogger logger)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            logger.Info("Sqlite {0} opening {1}", SQLiteConnection.SQLiteVersion, dbPath);

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 2000,
                SyncMode = SynchronizationModes.Full,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }
    }

    public class DbConnector : IDbConnector
    {
        private readonly ILogger _logger;

        public DbConnector(ILogger logger)
        {
            _logger = logger;
        }

        public Task<IDbConnection> Connect(string dbPath)
        {
            return SqliteExtensions.ConnectToDb(dbPath, _logger);
        }
    }
}