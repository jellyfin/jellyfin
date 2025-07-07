// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeDbContextFactoryInstance.cs
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Minimal stub for IDbContextFactory&lt;JellyfinDbContext&gt; for design-time use.
    /// </summary>
    internal sealed class DesignTimeDbContextFactoryInstance : IDbContextFactory<JellyfinDbContext>
    {
        private readonly IDesignTimeDbContextFactory<JellyfinDbContext> _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignTimeDbContextFactoryInstance"/> class.
        /// </summary>
        /// <param name="factory">The underlying design-time factory.</param>
        public DesignTimeDbContextFactoryInstance(IDesignTimeDbContextFactory<JellyfinDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Creates a new instance of <see cref="JellyfinDbContext"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="JellyfinDbContext"/>.</returns>
        public JellyfinDbContext CreateDbContext()
        {
            // Pass empty args, as the main factory's CreateDbContext(string[] args) will handle defaults
            return _factory.CreateDbContext(Array.Empty<string>());
        }
    }
}
