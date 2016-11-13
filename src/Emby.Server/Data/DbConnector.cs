using System;
using System.Data;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using Emby.Server.Core.Data;
using Microsoft.Data.Sqlite;

namespace Emby.Server.Data
{
    public class DbConnector : IDbConnector
    {
        private readonly ILogger _logger;

        public DbConnector(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IDbConnection> Connect(string dbPath, bool isReadOnly, bool enablePooling = false, int? cacheSize = null)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            //SQLiteConnection.SetMemoryStatus(false);
            
            var connectionstr = new SqliteConnectionStringBuilder
            {
                //PageSize = 4096,
                //CacheSize = cacheSize ?? 2000,
                //SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                //JournalMode = SQLiteJournalModeEnum.Wal,

                // This is causing crashing under linux
                //Pooling = enablePooling && Environment.OSVersion.Platform == PlatformID.Win32NT,
                //ReadOnly = isReadOnly,
                Cache = enablePooling ? SqliteCacheMode.Default : SqliteCacheMode.Private,
                Mode = isReadOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate
            };

            var connectionString = connectionstr.ConnectionString;

            var connection = new SqliteConnection(connectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }
    }
}