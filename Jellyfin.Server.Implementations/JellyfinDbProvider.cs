using System;
using Jellyfin.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Implementations
{
    /// <summary>
    /// Acts as an intermediate layer between the application's service provider and
    /// the rest of the application to provide access to a connection pool to the
    /// underlying database.
    /// </summary>
    public class JellyfinDbProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Creates a new instance of the <see cref="JellyfinDbProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application's service provider.</param>
        /// <param name="appPaths">The application paths.</param>
        public JellyfinDbProvider(IServiceProvider serviceProvider, IApplicationPaths appPaths)
        {
            _serviceProvider = serviceProvider;
            serviceProvider.GetService<JellyfinDb>().Database.EnsureCreated();
        }

        /// <summary>
        /// Pulls a connection to the database from the underlying pool.
        /// </summary>
        /// <returns>A connection to the database.</returns>
        public JellyfinDb GetConnection()
        {
            return _serviceProvider.GetService<JellyfinDb>();
        }
    }
}
