using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.MsSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <summary>
    /// The design time factory for <see cref="JellyfinDbContext"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal sealed class MsSqlDesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();
            optionsBuilder.UseSqlServer(string.Empty, f => f.MigrationsAssembly(GetType().Assembly));

            return new JellyfinDbContext(
                optionsBuilder.Options,
                NullLogger<JellyfinDbContext>.Instance,
                new MsSqlDatabaseProvider(NullLogger<MsSqlDatabaseProvider>.Instance),
                new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        }
    }
}
