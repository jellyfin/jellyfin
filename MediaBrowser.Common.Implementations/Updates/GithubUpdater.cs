using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Implementations.Updates
{
    public class GithubUpdater
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public GithubUpdater(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<CheckForUpdateResult> CheckForUpdateResult(string organzation, string repository, Version minVersion, PackageVersionClass updateLevel, string assetFilename, string packageName, string targetFilename, TimeSpan cacheLength, CancellationToken cancellationToken)
        {
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases", organzation, repository);

            var options = new HttpRequestOptions
            {
                Url = url,
                EnableKeepAlive = false,
                CancellationToken = cancellationToken,
                UserAgent = "Emby/3.0",
                BufferContent = false
            };

            if (cacheLength.Ticks > 0)
            {
                options.CacheMode = CacheMode.Unconditional;
                options.CacheLength = cacheLength;
            }

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var obj = _jsonSerializer.DeserializeFromStream<RootObject[]>(stream);

                return CheckForUpdateResult(obj, minVersion, updateLevel, assetFilename, packageName, targetFilename);
            }
        }

        private CheckForUpdateResult CheckForUpdateResult(RootObject[] obj, Version minVersion, PackageVersionClass updateLevel, string assetFilename, string packageName, string targetFilename)
        {
            if (updateLevel == PackageVersionClass.Release)
            {
                // Technically all we need to do is check that it's not pre-release
                // But let's addititional checks for -beta and -dev to handle builds that might be temporarily tagged incorrectly.
                obj = obj.Where(i => !i.prerelease && !i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase) && !i.name.EndsWith("-dev", StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            else if (updateLevel == PackageVersionClass.Beta)
            {
                obj = obj.Where(i => !i.prerelease || i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            else if (updateLevel == PackageVersionClass.Dev)
            {
                obj = obj.Where(i => !i.prerelease || i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase) || i.name.EndsWith("-dev", StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            var availableUpdate = obj
                .Select(i => CheckForUpdateResult(i, minVersion, assetFilename, packageName, targetFilename))
                .Where(i => i != null)
                .OrderByDescending(i => Version.Parse(i.AvailableVersion))
                .FirstOrDefault();

            return availableUpdate ?? new CheckForUpdateResult
            {
                IsUpdateAvailable = false
            };
        }

        private bool MatchesUpdateLevel(RootObject i, PackageVersionClass updateLevel)
        {
            if (updateLevel == PackageVersionClass.Beta)
            {
                return !i.prerelease || i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase);
            }
            if (updateLevel == PackageVersionClass.Dev)
            {
                return !i.prerelease || i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase) ||
                       i.name.EndsWith("-dev", StringComparison.OrdinalIgnoreCase);
            }

            // Technically all we need to do is check that it's not pre-release
            // But let's addititional checks for -beta and -dev to handle builds that might be temporarily tagged incorrectly.
            return !i.prerelease && !i.name.EndsWith("-beta", StringComparison.OrdinalIgnoreCase) &&
                   !i.name.EndsWith("-dev", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<RootObject>> GetLatestReleases(string organzation, string repository, string assetFilename, CancellationToken cancellationToken)
        {
            var list = new List<RootObject>();

            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases", organzation, repository);

            var options = new HttpRequestOptions
            {
                Url = url,
                EnableKeepAlive = false,
                CancellationToken = cancellationToken,
                UserAgent = "Emby/3.0",
                BufferContent = false
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var obj = _jsonSerializer.DeserializeFromStream<RootObject[]>(stream);

                obj = obj.Where(i => (i.assets ?? new List<Asset>()).Any(a => IsAsset(a, assetFilename))).ToArray();

                list.AddRange(obj.Where(i => MatchesUpdateLevel(i, PackageVersionClass.Release)).OrderByDescending(GetVersion).Take(1));
                list.AddRange(obj.Where(i => MatchesUpdateLevel(i, PackageVersionClass.Beta)).OrderByDescending(GetVersion).Take(1));
                list.AddRange(obj.Where(i => MatchesUpdateLevel(i, PackageVersionClass.Dev)).OrderByDescending(GetVersion).Take(1));

                return list;
            }
        }

        public Version GetVersion(RootObject obj)
        {
            Version version;
            if (!Version.TryParse(obj.tag_name, out version))
            {
                return new Version(1, 0);
            }

            return version;
        }

        private CheckForUpdateResult CheckForUpdateResult(RootObject obj, Version minVersion, string assetFilename, string packageName, string targetFilename)
        {
            Version version;
            if (!Version.TryParse(obj.tag_name, out version))
            {
                return null;
            }

            if (version < minVersion)
            {
                return null;
            }

            var asset = (obj.assets ?? new List<Asset>()).FirstOrDefault(i => IsAsset(i, assetFilename));

            if (asset == null)
            {
                return null;
            }

            return new CheckForUpdateResult
            {
                AvailableVersion = version.ToString(),
                IsUpdateAvailable = version > minVersion,
                Package = new PackageVersionInfo
                {
                    classification = obj.prerelease ?
                        (obj.name.EndsWith("-dev", StringComparison.OrdinalIgnoreCase) ? PackageVersionClass.Dev : PackageVersionClass.Beta) :
                        PackageVersionClass.Release,
                    name = packageName,
                    sourceUrl = asset.browser_download_url,
                    targetFilename = targetFilename,
                    versionStr = version.ToString(),
                    requiredVersionStr = "1.0.0",
                    description = obj.body,
                    infoUrl = obj.html_url
                }
            };
        }

        private bool IsAsset(Asset asset, string assetFilename)
        {
            var downloadFilename = Path.GetFileName(asset.browser_download_url) ?? string.Empty;

            if (downloadFilename.IndexOf(assetFilename, StringComparison.OrdinalIgnoreCase) != -1)
            {
                return true;
            }

            return string.Equals(assetFilename, downloadFilename, StringComparison.OrdinalIgnoreCase);
        }

        public class Uploader
        {
            public string login { get; set; }
            public int id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class Asset
        {
            public string url { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public object label { get; set; }
            public Uploader uploader { get; set; }
            public string content_type { get; set; }
            public string state { get; set; }
            public int size { get; set; }
            public int download_count { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string browser_download_url { get; set; }
        }

        public class Author
        {
            public string login { get; set; }
            public int id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class RootObject
        {
            public string url { get; set; }
            public string assets_url { get; set; }
            public string upload_url { get; set; }
            public string html_url { get; set; }
            public int id { get; set; }
            public string tag_name { get; set; }
            public string target_commitish { get; set; }
            public string name { get; set; }
            public bool draft { get; set; }
            public Author author { get; set; }
            public bool prerelease { get; set; }
            public string created_at { get; set; }
            public string published_at { get; set; }
            public List<Asset> assets { get; set; }
            public string tarball_url { get; set; }
            public string zipball_url { get; set; }
            public string body { get; set; }
        }
    }
}
