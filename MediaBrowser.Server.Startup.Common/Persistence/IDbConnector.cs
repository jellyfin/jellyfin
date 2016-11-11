using System.Data;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Startup.Common.Persistence
{
    public interface IDbConnector
    {
        Task<IDbConnection> Connect(string dbPath, bool isReadOnly, bool enablePooling = false, int? cacheSize = null);
    }
}
