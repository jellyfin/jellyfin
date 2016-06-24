using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.TV;
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
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Music
{
    public class FanartArtistProvider : IRemoteImageProvider, IHasOrder
    {
        internal readonly SemaphoreSlim FanArtResourcePool = new SemaphoreSlim(3, 3);
        internal const string ApiKey = "5c6b04c68e904cfed1e6cbc9a9e683d4";
        private const string FanArtBaseUrl = "https://webservice.fanart.tv/v3.1/music/{1}?api_key={0}";

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        internal static FanartArtistProvider Current;

        public FanartArtistProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;

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
            return item is MusicArtist;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Logo,
                ImageType.Art,
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var artist = (MusicArtist)item;

            var list = new List<RemoteImageInfo>();

            var artistMusicBrainzId = artist.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!String.IsNullOrEmpty(artistMusicBrainzId))
            {
                await EnsureArtistJson(artistMusicBrainzId, cancellationToken).ConfigureAwait(false);

                var artistJsonPath = GetArtistJsonPath(_config.CommonApplicationPaths, artistMusicBrainzId);

                try
                {
                    AddImages(list, artistJsonPath, cancellationToken);
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
                {

                }
            }

            var language = item.GetPreferredMetadataLanguage();

            var isLanguageEn = String.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            // Sort first by width to prioritize HD versions
            return list.OrderByDescending(i => i.Width ?? 0)
                .ThenByDescending(i =>
                {
                    if (String.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }
                    if (!isLanguageEn)
                    {
                        if (String.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            return 2;
                        }
                    }
                    if (String.IsNullOrEmpty(i.Language))
                    {
                        return isLanguageEn ? 3 : 2;
                    }
                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        /// <summary>
        /// Adds the images.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImages(List<RemoteImageInfo> list, string path, CancellationToken cancellationToken)
        {
            var obj = _jsonSerializer.DeserializeFromFile<FanartArtistResponse>(path);

            PopulateImages(list, obj.artistbackground, ImageType.Backdrop, 1920, 1080);
            PopulateImages(list, obj.artistthumb, ImageType.Primary, 500, 281);
            PopulateImages(list, obj.hdmusiclogo, ImageType.Logo, 800, 310);
            PopulateImages(list, obj.musicbanner, ImageType.Banner, 1000, 185);
            PopulateImages(list, obj.musiclogo, ImageType.Logo, 400, 155);
            PopulateImages(list, obj.hdmusicarts, ImageType.Art, 1000, 562);
            PopulateImages(list, obj.musicarts, ImageType.Art, 500, 281);
        }

        private Regex _regex_http = new Regex("^http://");
        private void PopulateImages(List<RemoteImageInfo> list,
            List<FanartArtistImage> images,
            ImageType type,
            int width,
            int height)
        {
            if (images == null)
            {
                return;
            }

            list.AddRange(images.Select(i =>
            {
                var url = i.url;

                if (!string.IsNullOrEmpty(url))
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
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = FanArtResourcePool
            });
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureArtistJson(string musicBrainzId, CancellationToken cancellationToken)
        {
            var jsonPath = GetArtistJsonPath(_config.ApplicationPaths, musicBrainzId);

            var fileInfo = _fileSystem.GetFileSystemInfo(jsonPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedTask;
                }
            }

            return DownloadArtistJson(musicBrainzId, cancellationToken);
        }

        /// <summary>
        /// Downloads the artist data.
        /// </summary>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        internal async Task DownloadArtistJson(string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, ApiKey, musicBrainzId);

            var clientKey = FanartSeriesProvider.Current.GetFanartOptions().UserApiKey;
            if (!string.IsNullOrWhiteSpace(clientKey))
            {
                url += "&client_key=" + clientKey;
            }

            var jsonPath = GetArtistJsonPath(_config.ApplicationPaths, musicBrainzId);

            _fileSystem.CreateDirectory(Path.GetDirectoryName(jsonPath));

            try
            {
                using (var response = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = FanArtResourcePool,
                    CancellationToken = cancellationToken

                }).ConfigureAwait(false))
                {
                    using (var saveFileStream = _fileSystem.GetFileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await response.CopyToAsync(saveFileStream).ConfigureAwait(false);
                    }
                }
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    _jsonSerializer.SerializeToFile(new FanartArtistResponse(), jsonPath);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="musicBrainzArtistId">The music brainz artist identifier.</param>
        /// <returns>System.String.</returns>
        private static string GetArtistDataPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = Path.Combine(GetArtistDataPath(appPaths), musicBrainzArtistId);

            return dataPath;
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetArtistDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "fanart-music");

            return dataPath;
        }

        internal static string GetArtistJsonPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = GetArtistDataPath(appPaths, musicBrainzArtistId);

            return Path.Combine(dataPath, "fanart.json");
        }


        public class FanartArtistImage
        {
            public string id { get; set; }
            public string url { get; set; }
            public string likes { get; set; }
            public string disc { get; set; }
            public string size { get; set; }
            public string lang { get; set; }
        }

        public class Album
        {
            public string release_group_id { get; set; }
            public List<FanartArtistImage> cdart { get; set; }
            public List<FanartArtistImage> albumcover { get; set; }
        }

        public class FanartArtistResponse
        {
            public string name { get; set; }
            public string mbid_id { get; set; }
            public List<FanartArtistImage> artistthumb { get; set; }
            public List<FanartArtistImage> artistbackground { get; set; }
            public List<FanartArtistImage> hdmusiclogo { get; set; }
            public List<FanartArtistImage> musicbanner { get; set; }
            public List<FanartArtistImage> musiclogo { get; set; }
            public List<FanartArtistImage> musicarts { get; set; }
            public List<FanartArtistImage> hdmusicarts { get; set; }
            public List<Album> albums { get; set; }
        }
    }
}
