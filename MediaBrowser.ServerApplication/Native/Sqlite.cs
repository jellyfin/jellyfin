using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class Sqlite
    /// </summary>
    public static class Sqlite
    {
        /// <summary>
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        public static async Task<IDbConnection> OpenDatabase(string dbPath)
        {
            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 4096,
                SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }
    }
}
