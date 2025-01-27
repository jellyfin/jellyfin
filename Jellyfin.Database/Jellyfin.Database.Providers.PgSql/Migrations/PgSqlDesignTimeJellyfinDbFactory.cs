using Jellyfin.Database.Providers.SqLite;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Providers.PgSql
{
    /// <summary>
    /// The design time factory for <see cref="JellyfinDbContext"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal sealed class PgSqlDesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();
            optionsBuilder.UseNpgsql(f => f.MigrationsAssembly(GetType().Assembly));

            return new JellyfinDbContext(
                optionsBuilder.Options,
                NullLogger<JellyfinDbContext>.Instance,
                new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance));
        }
    }
}
