using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Persistence;

namespace MediaBrowser.ServerApplication.Native
{
    public class DbConnector : IDbConnector
    {
        private readonly ILogger _logger;

        public DbConnector(ILogger logger)
        {
            _logger = logger;
        }

        public void BindSimilarityScoreFunction(IDbConnection connection)
        {
            SqliteExtensions.BindGetSimilarityScore(connection, _logger);
        }

        public async Task<IDbConnection> Connect(string dbPath)
        {
            try
            {
                return await SqliteExtensions.ConnectToDb(dbPath, _logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error opening database {0}", ex, dbPath);

                throw;
            }
        }
    }
}