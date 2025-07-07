// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeJellyfinDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

// Note: The stub classes DesignTimeDbContextFactoryInstance, DesignTimeDatabaseProvider,
// and DesignTimeLockingBehavior have been moved to their own files.

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Design-time factory for creating instances of <see cref="JellyfinDbContext"/>.
    /// This factory is used by Entity Framework Core tools during design-time operations like migrations.
    /// </summary>
    public class DesignTimeJellyfinDbContextFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="JellyfinDbContext"/>.
        /// </summary>
        /// <param name="args">Arguments provided by the design-time tools.</param>
        /// <returns>An instance of <see cref="JellyfinDbContext"/>.</returns>
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

            // Use a dummy connection string for design-time.
            // EF Core tools primarily need to know the provider and model.
            // The actual runtime connection string comes from Jellyfin's configuration.
            // Crucially, specify the assembly where migration files are actually stored.
            optionsBuilder.UseSqlite(
                "Data Source=design_time_temp.db",
                sqliteOptionsAction => sqliteOptionsAction.MigrationsAssembly("Jellyfin.Database.Providers.Sqlite"));

            // Provide stubs/minimal implementations for other DbContext constructor dependencies.
            var logger = new NullLogger<JellyfinDbContext>();
            var dummyProvider = new DesignTimeDatabaseProvider();
            var dummyLockingBehavior = new DesignTimeLockingBehavior();

            return new JellyfinDbContext(optionsBuilder.Options, logger, dummyProvider, dummyLockingBehavior);
        }
    }
}
