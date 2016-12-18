using System.Threading.Tasks;

namespace Emby.Server.Implementations.Migrations
{
    public interface IVersionMigration
    {
        Task Run();
    }
}
