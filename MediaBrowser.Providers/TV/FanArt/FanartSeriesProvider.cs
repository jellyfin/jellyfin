using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Music;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class FanartSeriesProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _json;

        private const string FanArtBaseUrl = "https://webservice.fanart.tv/v3/tv/{1}?api_key={0}";
        // &client_key=52c813aa7b8c8b3bb87f4797532a2f8c

        internal static FanartSeriesProvider Current { get; private set; }

        public FanartSeriesProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem, IJsonSerializer json)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _json = json;

            Current = this;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "FanArt"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Thumb,
                ImageType.Art,
                ImageType.Logo,
                ImageType.Backdrop,
                ImageType.Banner
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var series = (Series)item;

            var id = series.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(id))
            {
                // Bad id entered
                try
                {
                    await EnsureSeriesJson(id, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpException ex)
                {
                    if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }

                var path = GetFanartJsonPath(id);

                try
                {
                    AddImages(list, path, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // No biggie. Don't blow up
                }
                catch (DirectoryNotFoundException)
                {
                    // No biggie. Don't blow up
                }
            }

            var language = item.GetPreferredMetadataLanguage();

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            // Sort first by width to prioritize HD versions
            return list.OrderByDescending(i => i.Width ?? 0)
                .ThenByDescending(i =>
                {
                    if (string.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }
                    if (!isLanguageEn)
                    {
                        if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            return 2;
                        }
                    }
                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return isLanguageEn ? 3 : 2;
                    }
                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        private void AddImages(List<RemoteImageInfo> list, string path, CancellationToken cancellationToken)
        {
            var root = _json.DeserializeFromFile<RootObject>(path);

            AddImages(list, root, cancellationToken);
        }

        private void AddImages(List<RemoteImageInfo> list, RootObject obj, CancellationToken cancellationToken)
        {
            PopulateImages(list, obj.hdtvlogo, ImageType.Logo, 800, 310);
            PopulateImages(list, obj.hdclearart, ImageType.Art, 1000, 562);
            PopulateImages(list, obj.clearlogo, ImageType.Logo, 400, 155);
            PopulateImages(list, obj.clearart, ImageType.Art, 500, 281);
            PopulateImages(list, obj.showbackground, ImageType.Backdrop, 1920, 1080, true);
            PopulateImages(list, obj.seasonthumb, ImageType.Thumb, 500, 281);
            PopulateImages(list, obj.tvthumb, ImageType.Thumb, 500, 281);
            PopulateImages(list, obj.tvbanner, ImageType.Banner, 1000, 185);
            PopulateImages(list, obj.tvposter, ImageType.Primary, 1000, 1426);
        }

        private Regex _regex_http = new Regex("^http://");
        private void PopulateImages(List<RemoteImageInfo> list,
            List<Image> images,
            ImageType type,
            int width,
            int height,
            bool allowSeasonAll = false)
        {
            if (images == null)
            {
                return;
            }

            list.AddRange(images.Select(i =>
            {
                var url = i.url;
                var season = i.season;

                var isSeasonValid = string.IsNullOrEmpty(season) ||
                    (allowSeasonAll && string.Equals(season, "all", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(url) && isSeasonValid)
                {
                    var likesString = i.likes;
                    int likes;

                    var info = new RemoteImageInfo
                    {
                        RatingType = RatingType.Likes,
                        Type = type,
                        Width = width,
                        Height = height,
                        ProviderName = Name,
                        Url = _regex_http.Replace(url, "https://", 1),
                        Language = i.lang
                    };

                    if (!string.IsNullOrEmpty(likesString) && int.TryParse(likesString, NumberStyles.Any, _usCulture, out likes))
                    {
                        info.CommunityRating = likes;
                    }

                    return info;
                }

                return null;
            }).Where(i => i != null));
        }

        public int Order
        {
            get { return 1; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = FanartArtistProvider.Current.FanArtResourcePool
            });
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "fanart-tv");

            return dataPath;
        }

        public string GetFanartJsonPath(string tvdbId)
        {
            var dataPath = GetSeriesDataPath(_config.ApplicationPaths, tvdbId);
            return Path.Combine(dataPath, "fanart.json");
        }

        private readonly SemaphoreSlim _ensureSemaphore = new SemaphoreSlim(1, 1);
        internal async Task EnsureSeriesJson(string tvdbId, CancellationToken cancellationToken)
        {
            var path = GetFanartJsonPath(tvdbId);

            // Only allow one thread in here at a time since every season will be calling this method, possibly concurrently
            await _ensureSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var fileInfo = _fileSystem.GetFileSystemInfo(path);

                if (fileInfo.Exists)
                {
                    if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                    {
                        return;
                    }
                }

                await DownloadSeriesJson(tvdbId, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _ensureSemaphore.Release();
            }
        }

        public FanartOptions GetFanartOptions()
        {
            return _config.GetConfiguration<FanartOptions>("fanart");
        }

        /// <summary>
        /// Downloads the series json.
        /// </summary>
        /// <param name="tvdbId">The TVDB identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadSeriesJson(string tvdbId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, FanartArtistProvider.ApiKey, tvdbId);

            var clientKey = GetFanartOptions().UserApiKey;
            if (!string.IsNullOrWhiteSpace(clientKey))
            {
                url += "&client_key=" + clientKey;
            }

            var path = GetFanartJsonPath(tvdbId);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            try
            {
                using (var response = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = FanartArtistProvider.Current.FanArtResourcePool,
                    CancellationToken = cancellationToken

                }).ConfigureAwait(false))
                {
                    using (var fileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await response.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }
            }
            catch (HttpException exception)
            {
                if (exception.StatusCode.HasValue && exception.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    // If the user has automatic updates enabled, save a dummy object to prevent repeated download attempts
                    _json.SerializeToFile(new RootObject(), path);

                    return;
                }

                throw;
            }
        }

        public class Image
        {
            public string id { get; set; }
            public string url { get; set; }
            public string lang { get; set; }
            public string likes { get; set; }
            public string season { get; set; }
        }

        public class RootObject
        {
            public string name { get; set; }
            public string thetvdb_id { get; set; }
            public List<Image> clearlogo { get; set; }
            public List<Image> hdtvlogo { get; set; }
            public List<Image> clearart { get; set; }
            public List<Image> showbackground { get; set; }
            public List<Image> tvthumb { get; set; }
            public List<Image> seasonposter { get; set; }
            public List<Image> seasonthumb { get; set; }
            public List<Image> hdclearart { get; set; }
            public List<Image> tvbanner { get; set; }
            public List<Image> characterart { get; set; }
            public List<Image> tvposter { get; set; }
            public List<Image> seasonbanner { get; set; }
        }
    }

    public class FanartConfigStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                     Key = "fanart",
                     ConfigurationType = typeof(FanartOptions)
                }
            };
        }
    }
}
