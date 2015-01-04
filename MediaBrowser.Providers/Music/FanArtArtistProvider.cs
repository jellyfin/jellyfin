using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class FanartArtistProvider : IRemoteImageProvider, IHasChangeMonitor, IHasOrder
    {
        internal readonly SemaphoreSlim FanArtResourcePool = new SemaphoreSlim(3, 3);
        internal const string ApiKey = "5c6b04c68e904cfed1e6cbc9a9e683d4";
        private const string FanArtBaseUrl = "http://api.fanart.tv/webservice/artist/{0}/{1}/xml/all/1/1";

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        internal static FanartArtistProvider Current;

        public FanartArtistProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
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
                await EnsureArtistXml(artistMusicBrainzId, cancellationToken).ConfigureAwait(false);

                var artistXmlPath = GetArtistXmlPath(_config.CommonApplicationPaths, artistMusicBrainzId);

                try
                {
                    AddImages(list, artistXmlPath, cancellationToken);
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
        /// <param name="xmlPath">The XML path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
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
                                case "music":
                                    {
                                        using (var subReader = reader.ReadSubtree())
                                        {
                                            AddImagesFromMusicNode(list, subReader, cancellationToken);
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

        /// <summary>
        /// Adds the images from music node.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImagesFromMusicNode(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "hdmusiclogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Logo, 800, 310, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "musiclogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Logo, 400, 155, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "artistbackgrounds":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Backdrop, 1920, 1080, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "hdmusicarts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Art, 1000, 562, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "musicarts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Art, 500, 281, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "hdmusicbanners":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Banner, 1000, 185, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "musicbanners":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Banner, 1000, 185, subReader, cancellationToken);
                                }
                                break;
                            }
                        case "artistthumbs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromImageTypeNode(list, ImageType.Primary, 1000, 1000, subReader, cancellationToken);
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

        /// <summary>
        /// Adds the images from albums node.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="type">The type.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImagesFromImageTypeNode(List<RemoteImageInfo> list, ImageType type, int width, int height, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "hdmusiclogo":
                        case "musiclogo":
                        case "artistbackground":
                        case "hdmusicart":
                        case "musicart":
                        case "hdmusicbanner":
                        case "musicbanner":
                        case "artistthumb":
                            {
                                AddImage(list, reader, type, width, height);
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

        /// <summary>
        /// Adds the image.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="type">The type.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        private void AddImage(List<RemoteImageInfo> list, XmlReader reader, ImageType type, int width, int height)
        {
            var url = reader.GetAttribute("url");

            var size = reader.GetAttribute("size");

            if (!String.IsNullOrEmpty(size))
            {
                int sizeNum;
                if (Int32.TryParse(size, NumberStyles.Any, _usCulture, out sizeNum))
                {
                    width = sizeNum;
                    height = sizeNum;
                }
            }

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

            if (!String.IsNullOrEmpty(likesString) && Int32.TryParse(likesString, NumberStyles.Any, _usCulture, out likes))
            {
                info.CommunityRating = likes;
            }

            list.Add(info);
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

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (!_config.Configuration.EnableFanArtUpdates)
            {
                return false;
            }

            var id = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!String.IsNullOrEmpty(id))
            {
                // Process images
                var artistXmlPath = GetArtistXmlPath(_config.CommonApplicationPaths, id);

                var fileInfo = new FileInfo(artistXmlPath);

                return !fileInfo.Exists || _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
            }

            return false;
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureArtistXml(string musicBrainzId, CancellationToken cancellationToken)
        {
            var xmlPath = GetArtistXmlPath(_config.ApplicationPaths, musicBrainzId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedTask;
                }
            }

            return DownloadArtistXml(musicBrainzId, cancellationToken);
        }

        /// <summary>
        /// Downloads the artist XML.
        /// </summary>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        internal async Task DownloadArtistXml(string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, ApiKey, musicBrainzId);

            var xmlPath = GetArtistXmlPath(_config.ApplicationPaths, musicBrainzId);

            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

            using (var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
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

        internal static string GetArtistXmlPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = GetArtistDataPath(appPaths, musicBrainzArtistId);

            return Path.Combine(dataPath, "fanart.xml");
        }
    }
}
