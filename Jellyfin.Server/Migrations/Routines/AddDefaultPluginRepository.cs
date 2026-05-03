using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to initialize system configuration with the default plugin repository.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [JellyfinMigration("2025-04-20T09:00:00", nameof(AddDefaultPluginRepository), "EB58EBEE-9514-4B9B-8225-12E1A40020DF", RunMigrationOnSetup = true)]
    public class AddDefaultPluginRepository : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly IWritableOptions<ServerConfiguration> _serverConfiguration;

        private readonly RepositoryInfo _defaultRepositoryInfo = new RepositoryInfo
        {
            Name = "Jellyfin Stable",
            Url = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AddDefaultPluginRepository"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the server configuration.</param>
        public AddDefaultPluginRepository(IWritableOptions<ServerConfiguration> serverConfigurationManager)
        {
            _serverConfiguration = serverConfigurationManager;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _serverConfiguration.Update(value =>
            {
                value.PluginRepositories = new[] { _defaultRepositoryInfo };
            });
        }
    }
}
