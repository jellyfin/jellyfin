using System.Threading.Tasks;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public interface IVersionMigration
    {
        Task Run();
    }
}
