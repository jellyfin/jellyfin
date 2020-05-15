using System;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinDbProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application's service provider.</param>
        public JellyfinDbProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            serviceProvider.GetService<JellyfinDb>().Database.Migrate();
        }

        /// <summary>
        /// Creates a new <see cref="JellyfinDb"/> context.
        /// </summary>
        /// <returns>The newly created context.</returns>
        public JellyfinDb CreateContext()
        {
            return _serviceProvider.GetRequiredService<JellyfinDb>();
        }
    }
}
