using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <summary>
    /// The design time factory for <see cref="SqliteJellyfinDbContext"/>.
    /// This is only used for the creation of migrations and not during runtime.
    /// </summary>
    internal sealed class SqliteDesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<SqliteJellyfinDbContext>
    {
        public SqliteJellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteJellyfinDbContext>();
            optionsBuilder.UseSqlite("Data Source=jellyfin.db", f => f.MigrationsAssembly(GetType().Assembly));

            return new SqliteJellyfinDbContext(
                optionsBuilder.Options,
                NullLogger<JellyfinDbContext>.Instance,
                new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
                new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        }
    }
}
