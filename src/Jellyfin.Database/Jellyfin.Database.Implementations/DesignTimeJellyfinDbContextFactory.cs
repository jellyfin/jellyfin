// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeJellyfinDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;
using Jellyfin.Database.Implementations.Locking; // For IEntityFrameworkCoreLockingBehavior
// IJellyfinDatabaseProvider is in Jellyfin.Database.Implementations, which is this file's namespace

namespace Jellyfin.Database.Implementations
{
    public class DesignTimeJellyfinDbContextFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

            // This connection string is primarily for schema tooling.
            // The actual runtime connection string will come from Jellyfin's configuration.
            optionsBuilder.UseSqlite("Data Source=design_time_temp.db",
                sqliteOptionsAction => sqliteOptionsAction.MigrationsAssembly("Jellyfin.Database.Providers.Sqlite"));

            var logger = new NullLogger<JellyfinDbContext>();

            // For these dummy providers, we need to ensure they are not null if the DbContext constructor
            // performs any null checks. Let's use very simple stub implementations if 'null' causes issues.
            // However, to start, we'll try with null and see if EF Core's design-time context creation is tolerant.
            // If not, we'll need to define simple stub classes for these interfaces.
            IJellyfinDatabaseProvider dummyProvider = null;
            IEntityFrameworkCoreLockingBehavior dummyLockingBehavior = null;

            // This is the constructor call that might be sensitive to the dummy providers.
            return new JellyfinDbContext(optionsBuilder.Options, logger, dummyProvider, dummyLockingBehavior);
        }
    }
}
