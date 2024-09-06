using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to initialize system configuration with the default plugin repository.
    /// </summary>
    public class AddDefaultPluginRepository : IMigrationRoutine
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;

        private readonly RepositoryInfo _defaultRepositoryInfo = new RepositoryInfo
        {
            Name = "Jellyfin Stable",
            Url = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AddDefaultPluginRepository"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public AddDefaultPluginRepository(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("EB58EBEE-9514-4B9B-8225-12E1A40020DF");

        /// <inheritdoc/>
        public string Name => "AddDefaultPluginRepository";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => true;

        /// <inheritdoc/>
        public void Perform()
        {
            _serverConfigurationManager.Configuration.PluginRepositories = new[] { _defaultRepositoryInfo };
            _serverConfigurationManager.SaveConfiguration();
        }
    }
}
