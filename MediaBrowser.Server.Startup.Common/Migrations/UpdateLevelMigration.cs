using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
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

        public UpdateLevelMigration(IServerConfigurationManager config, IServerApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer, string releaseAssetFilename)
        {
            _config = config;
            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _releaseAssetFilename = releaseAssetFilename;
        }

        public async void Run()
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

                if (updateLevel == PackageVersionClass.Dev)
                {
                    // It's already dev, there's nothing to check
                    return;
                }

                await CheckVersion(currentVersion, updateLevel, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private async Task CheckVersion(Version currentVersion, PackageVersionClass updateLevel, CancellationToken cancellationToken)
        {
            var releases = await new GithubUpdater(_httpClient, _jsonSerializer, TimeSpan.FromMinutes(5))
                .GetLatestReleases("MediaBrowser", "Emby", _releaseAssetFilename, cancellationToken).ConfigureAwait(false);

            var newUpdateLevel = updateLevel;

            // If the current version is later than current stable, set the update level to beta
            if (releases.Count >= 1)
            {
                var release = releases[0];
                var version = ParseVersion(release.tag_name);
                if (version != null && currentVersion > version)
                {
                    newUpdateLevel = PackageVersionClass.Beta;
                }
            }

            // If the current version is later than current beta, set the update level to dev
            if (releases.Count >= 2)
            {
                var release = releases[1];
                var version = ParseVersion(release.tag_name);
                if (version != null && currentVersion > version)
                {
                    newUpdateLevel = PackageVersionClass.Dev;
                }
            }

            if (newUpdateLevel != updateLevel)
            {
                _config.Configuration.SystemUpdateLevel = newUpdateLevel;
                _config.SaveConfiguration();
            }
        }

        private Version ParseVersion(string versionString)
        {
            var parts = versionString.Split('.');
            if (parts.Length == 3)
            {
                versionString += ".0";
            }

            Version version;
            Version.TryParse(versionString, out version);

            return version;
        }
    }
}
