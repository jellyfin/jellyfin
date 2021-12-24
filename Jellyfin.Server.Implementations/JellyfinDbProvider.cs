using System;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations
{
    /// <summary>
    /// Factory class for generating new <see cref="JellyfinDb"/> instances.
    /// </summary>
    public class JellyfinDbProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<JellyfinDbProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinDbProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application's service provider.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="logger">The logger.</param>
        public JellyfinDbProvider(IServiceProvider serviceProvider, IApplicationPaths appPaths, ILogger<JellyfinDbProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _appPaths = appPaths;
            _logger = logger;

            using var jellyfinDb = CreateContext();
            if (jellyfinDb.Database.GetPendingMigrations().Any())
            {
                _logger.LogInformation("There are pending EFCore migrations in the database. Applying... (This may take a while, do not stop Jellyfin)");
                jellyfinDb.Database.Migrate();
                _logger.LogInformation("EFCore migrations applied successfully");
            }
        }

        /// <summary>
        /// Creates a new <see cref="JellyfinDb"/> context.
        /// </summary>
        /// <returns>The newly created context.</returns>
        public JellyfinDb CreateContext()
        {
            var contextOptions = new DbContextOptionsBuilder<JellyfinDb>().UseSqlite($"Filename={Path.Combine(_appPaths.DataPath, "jellyfin.db")}");
            return ActivatorUtilities.CreateInstance<JellyfinDb>(_serviceProvider, contextOptions.Options);
        }
    }
}
