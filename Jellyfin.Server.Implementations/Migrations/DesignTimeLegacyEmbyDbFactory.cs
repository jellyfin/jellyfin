using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// The design time factory for <see cref="LegacyEmbyDbContext"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal class DesignTimeLegacyEmbyDbFactory : IDesignTimeDbContextFactory<LegacyEmbyDbContext>
    {
        public LegacyEmbyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LegacyEmbyDbContext>();
            optionsBuilder.UseSqlite("Data Source=library.db");

            return new LegacyEmbyDbContext(optionsBuilder.Options);
        }
    }
}
