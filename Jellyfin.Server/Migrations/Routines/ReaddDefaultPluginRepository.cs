using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to initialize system configuration with the default plugin repository.
    /// </summary>
    public class ReaddDefaultPluginRepository : IMigrationRoutine
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;

        private readonly RepositoryInfo _defaultRepositoryInfo = new RepositoryInfo
        {
            Name = "Jellyfin Stable",
            Url = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaddDefaultPluginRepository"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ReaddDefaultPluginRepository(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("5F86E7F6-D966-4C77-849D-7A7B40B68C4E");

        /// <inheritdoc/>
        public string Name => "ReaddDefaultPluginRepository";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => true;

        /// <inheritdoc/>
        public void Perform()
        {
            // Only add if repository list is empty
            if (_serverConfigurationManager.Configuration.PluginRepositories.Length == 0)
            {
                _serverConfigurationManager.Configuration.PluginRepositories = new[] { _defaultRepositoryInfo };
                _serverConfigurationManager.SaveConfiguration();
            }
        }
    }
}
