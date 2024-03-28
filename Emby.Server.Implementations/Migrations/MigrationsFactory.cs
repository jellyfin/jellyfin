using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.Migrations
{
    /// <summary>
    /// A factory that can find a persistent file of the migration configuration, which lists all applied migrations.
    /// </summary>
    public class MigrationsFactory : IConfigurationFactory
    {
        /// <inheritdoc/>
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new MigrationsListStore()
            };
        }
    }
}
