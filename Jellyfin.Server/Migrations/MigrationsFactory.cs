using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// A factory that teachs Jellyfin how to find a peristent file which lists all applied migrations.
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
