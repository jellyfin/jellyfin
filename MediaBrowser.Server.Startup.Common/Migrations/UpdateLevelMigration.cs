using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class UpdateLevelMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _releaseAssetFilename;
        private readonly ILogger _logger;

        public UpdateLevelMigration(IServerConfigurationManager config, IServerApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer, string releaseAssetFilename, ILogger logger)
        {
            _config = config;
            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _releaseAssetFilename = releaseAssetFilename;
            _logger = logger;
        }

        public async Task Run()
        {
            var lastVersion = _config.Configuration.LastVersion;
            var currentVersion = _appHost.ApplicationVersion;

            if (string.Equals(lastVersion, currentVersion.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var updateLevel = _config.Configuration.SystemUpdateLevel;

                await CheckVersion(currentVersion, updateLevel, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in update migration", ex);
            }
        }

        private async Task CheckVersion(Version currentVersion, PackageVersionClass currentUpdateLevel, CancellationToken cancellationToken)
        {
            var releases = await new GithubUpdater(_httpClient, _jsonSerializer)
                .GetLatestReleases("MediaBrowser", "Emby", _releaseAssetFilename, cancellationToken).ConfigureAwait(false);

            var newUpdateLevel = GetNewUpdateLevel(currentVersion, currentUpdateLevel, releases);

            if (newUpdateLevel != currentUpdateLevel)
            {
                _config.Configuration.SystemUpdateLevel = newUpdateLevel;
                _config.SaveConfiguration();
            }
        }

        private PackageVersionClass GetNewUpdateLevel(Version currentVersion, PackageVersionClass currentUpdateLevel, List<GithubUpdater.RootObject> releases)
        {
            var newUpdateLevel = currentUpdateLevel;

            // If the current version is later than current stable, set the update level to beta
            if (releases.Count >= 1)
            {
                var release = releases[0];
                var version = ParseVersion(release.tag_name);
                if (version != null)
                {
                    if (currentVersion > version)
                    {
                        newUpdateLevel = PackageVersionClass.Beta;
                    }
                    else
                    {
                        return PackageVersionClass.Release;
                    }
                }
            }

            // If the current version is later than current beta, set the update level to dev
            if (releases.Count >= 2)
            {
                var release = releases[1];
                var version = ParseVersion(release.tag_name);
                if (version != null)
                {
                    if (currentVersion > version)
                    {
                        newUpdateLevel = PackageVersionClass.Dev;
                    }
                    else
                    {
                        return PackageVersionClass.Beta;
                    }
                }
            }

            return newUpdateLevel;
        }

        private Version ParseVersion(string versionString)
        {
            if (!string.IsNullOrWhiteSpace(versionString))
            {
                var parts = versionString.Split('.');
                if (parts.Length == 3)
                {
                    versionString += ".0";
                }
            }

            Version version;
            Version.TryParse(versionString, out version);

            return version;
        }
    }
}
