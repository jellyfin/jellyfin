using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteExtensions
    /// </summary>
    public static class SqliteExtensions
    {
        /// <summary>
        /// Connects to db.
        /// </summary>
        public static async Task<IDbConnection> ConnectToDb(string dbPath, bool isReadOnly, bool enablePooling, int? cacheSize, ILogger logger)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            SQLiteConnection.SetMemoryStatus(false);

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = cacheSize ?? 2000,
                SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal,

                // This is causing crashing under linux
                Pooling = enablePooling && Environment.OSVersion.Platform == PlatformID.Win32NT,
                ReadOnly = isReadOnly
            };

            var connectionString = connectionstr.ConnectionString;

            if (!enablePooling)
            {
                logger.Info("Sqlite {0} opening {1}", SQLiteConnection.SQLiteVersion, connectionString);
            }

            var connection = new SQLiteConnection(connectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }
    }
}
