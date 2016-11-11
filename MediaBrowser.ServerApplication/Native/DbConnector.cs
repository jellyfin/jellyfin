using System.Data;
using System.Threading.Tasks;
using Emby.Server.Core.Data;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.ServerApplication.Native
{
    public class DbConnector : IDbConnector
    {
        private readonly ILogger _logger;

        public DbConnector(ILogger logger)
        {
            _logger = logger;
        }

        public Task<IDbConnection> Connect(string dbPath, bool isReadOnly, bool enablePooling = false, int? cacheSize = null)
        {
            return SqliteExtensions.ConnectToDb(dbPath, isReadOnly, enablePooling, cacheSize, _logger);
        }
    }
}