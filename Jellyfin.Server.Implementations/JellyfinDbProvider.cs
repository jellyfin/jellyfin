using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Implementations
{
    /// <summary>
    /// Factory class for generating new <see cref="JellyfinDb"/> instances.
    /// </summary>
    public class JellyfinDbProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinDbProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application's service provider.</param>
        /// <param name="appPaths">The application paths.</param>
        public JellyfinDbProvider(IServiceProvider serviceProvider, IApplicationPaths appPaths)
        {
            _serviceProvider = serviceProvider;
            _appPaths = appPaths;

            using var jellyfinDb = CreateContext();
            jellyfinDb.Database.Migrate();
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
