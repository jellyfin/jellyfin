using System.Threading.Tasks;

namespace Emby.Server.Core.Migrations
{
    public interface IVersionMigration
    {
        Task Run();
    }
}
