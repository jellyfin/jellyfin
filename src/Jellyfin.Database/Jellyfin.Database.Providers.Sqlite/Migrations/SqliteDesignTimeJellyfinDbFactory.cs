using Jellyfin.Database.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// The design time factory for <see cref="JellyfinDbContext"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal sealed class SqliteDesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();
            optionsBuilder.UseSqlite("Data Source=jellyfin.db", f => f.MigrationsAssembly(GetType().Assembly));

            return new JellyfinDbContext(
                optionsBuilder.Options,
                NullLogger<JellyfinDbContext>.Instance,
                new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance));
        }
    }
}
