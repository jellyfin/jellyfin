using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// The design time factory for <see cref="JellyfinDb"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal class DesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDb>
    {
        public JellyfinDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDb>();
            optionsBuilder.UseSqlite("Data Source=jellyfin.db");

            return new JellyfinDb(optionsBuilder.Options);
        }
    }
}
