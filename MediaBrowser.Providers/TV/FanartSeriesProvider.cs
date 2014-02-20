using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Music;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    public class FanartSeriesProvider : IRemoteImageProvider, IHasOrder, IHasChangeMonitor
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/series/{0}/{1}/xml/all/1/1";

        internal static FanartSeriesProvider Current { get; private set; }

        public FanartSeriesProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;

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
                await EnsureSeriesXml(id, cancellationToken).ConfigureAwait(false);

                var xmlPath = GetFanartXmlPath(id);

                try
                {
                    AddImages(list, xmlPath, cancellationToken);
                }
                catch (FileNotFoundException)
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

        private void AddImages(List<RemoteImageInfo> list, string xmlPath, CancellationToken cancellationToken)
        {
            using (var streamReader = new StreamReader(xmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "series":
                                    {
                                        using (var subReader = reader.ReadSubtree())
                                        {
                                            AddImages(list, subReader, cancellationToken);
                                        }
                                        break;
                                    }

                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void AddImages(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "hdtvlogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Logo, 800, 310);
                                }
                                break;
                            }
                        case "hdcleararts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Art, 1000, 562);
                                }
                                break;
                            }
                        case "clearlogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Logo, 400, 155);
                                }
                                break;
                            }
                        case "cleararts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Art, 500, 281);
                                }
                                break;
                            }
                        case "showbackgrounds":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Backdrop, 1920, 1080, true);
                                }
                                break;
                            }
                        case "seasonthumbs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Thumb, 500, 281);
                                }
                                break;
                            }
                        case "tvthumbs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Thumb, 500, 281);
                                }
                                break;
                            }
                        case "tvbanners":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Banner, 1000, 185);
                                }
                                break;
                            }
                        case "tvposters":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Primary, 1000, 1426);
                                }
                                break;
                            }
                        default:
                            {
                                using (reader.ReadSubtree())
                                {
                                }
                                break;
                            }
                    }
                }
            }
        }

        private void PopulateImageCategory(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken, ImageType type, int width, int height, bool allowSeasonAll = false)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "hdtvlogo":
                        case "hdclearart":
                        case "clearlogo":
                        case "clearart":
                        case "showbackground":
                        case "seasonthumb":
                        case "tvthumb":
                        case "tvbanner":
                        case "tvposter":
                            {
                                var url = reader.GetAttribute("url");
                                var season = reader.GetAttribute("season");

                                var isSeasonValid = string.IsNullOrEmpty(season) ||
                                    (allowSeasonAll && string.Equals(season, "all", StringComparison.OrdinalIgnoreCase));

                                if (!string.IsNullOrEmpty(url) && isSeasonValid)
                                {
                                    var likesString = reader.GetAttribute("likes");
                                    int likes;

                                    var info = new RemoteImageInfo
                                    {
                                        RatingType = RatingType.Likes,
                                        Type = type,
                                        Width = width,
                                        Height = height,
                                        ProviderName = Name,
                                        Url = url,
                                        Language = reader.GetAttribute("lang")
                                    };

                                    if (!string.IsNullOrEmpty(likesString) && int.TryParse(likesString, NumberStyles.Any, _usCulture, out likes))
                                    {
                                        info.CommunityRating = likes;
                                    }

                                    list.Add(info);
                                }

                                break;
                            }
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
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
                ResourcePool = FanartArtistProvider.FanArtResourcePool
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
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-tv");

            return dataPath;
        }

        public string GetFanartXmlPath(string tvdbId)
        {
            var dataPath = GetSeriesDataPath(_config.ApplicationPaths, tvdbId);
            return Path.Combine(dataPath, "fanart.xml");
        }

        private readonly SemaphoreSlim _ensureSemaphore = new SemaphoreSlim(1, 1);
        internal async Task EnsureSeriesXml(string tvdbId, CancellationToken cancellationToken)
        {
            var xmlPath = GetFanartXmlPath(tvdbId);

            // Only allow one thread in here at a time since every season will be calling this method, possibly concurrently
            await _ensureSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

                if (fileInfo.Exists)
                {
                    if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                    {
                        return;
                    }
                }

                await DownloadSeriesXml(tvdbId, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _ensureSemaphore.Release();
            }
        }

        /// <summary>
        /// Downloads the series XML.
        /// </summary>
        /// <param name="tvdbId">The TVDB id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadSeriesXml(string tvdbId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, FanartArtistProvider.ApiKey, tvdbId);

            var xmlPath = GetFanartXmlPath(tvdbId);

            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

            using (var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanartArtistProvider.FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (!_config.Configuration.EnableFanArtUpdates)
            {
                return false;
            }

            var tvdbId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!String.IsNullOrEmpty(tvdbId))
            {
                // Process images
                var imagesXmlPath = GetFanartXmlPath(tvdbId);

                var fileInfo = new FileInfo(imagesXmlPath);

                return !fileInfo.Exists || _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
            }

            return false;
        }
    }
}
