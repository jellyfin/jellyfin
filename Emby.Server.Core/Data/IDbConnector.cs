using System.Data;
using System.Threading.Tasks;

namespace Emby.Server.Core.Data
{
    public interface IDbConnector
    {
        Task<IDbConnection> Connect(string dbPath, bool isReadOnly, bool enablePooling = false, int? cacheSize = null);
    }
}
