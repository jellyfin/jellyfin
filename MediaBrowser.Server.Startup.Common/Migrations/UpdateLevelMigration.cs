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

            if (releases.Count >= 2)
            {
                var beta = releases[1];
                Version version;
                if (Version.TryParse(beta.tag_name, out version))
                {
                    if (currentVersion >= version)
                    {
                        newUpdateLevel = PackageVersionClass.Beta;
                    }
                }
            }

            if (releases.Count >= 3)
            {
                var dev = releases[2];
                Version version;
                if (Version.TryParse(dev.tag_name, out version))
                {
                    if (currentVersion >= version)
                    {
                        newUpdateLevel = PackageVersionClass.Dev;
                    }
                }
            }

            if (newUpdateLevel != updateLevel)
            {
                _config.Configuration.SystemUpdateLevel = newUpdateLevel;
                _config.SaveConfiguration();
            }
        }
    }
}
