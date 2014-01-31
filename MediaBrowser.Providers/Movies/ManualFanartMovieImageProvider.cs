using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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
using MediaBrowser.Providers.Music;

namespace MediaBrowser.Providers.Movies
{
    public class ManualFanartMovieImageProvider : IRemoteImageProvider, IHasChangeMonitor, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public ManualFanartMovieImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
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
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            return item is Movie || item is BoxSet || item is MusicVideo;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Thumb,
                ImageType.Art,
                ImageType.Logo,
                ImageType.Disc,
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var baseItem = (BaseItem)item;
            var list = new List<RemoteImageInfo>();

            var movieId = baseItem.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(movieId))
            {
                await FanartMovieProvider.Current.EnsureMovieXml(movieId, cancellationToken).ConfigureAwait(false);

                var xmlPath = FanartMovieProvider.Current.GetFanartXmlPath(movieId);

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
                .ThenByDescending(i => i.CommunityRating ?? 0);
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
                                case "movie":
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
                        case "hdmoviecleararts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Art, 1000, 562);
                                }
                                break;
                            }
                        case "hdmovielogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Logo, 800, 310);
                                }
                                break;
                            }
                        case "moviediscs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Disc, 1000, 1000);
                                }
                                break;
                            }
                        case "movieposters":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Primary, 1000, 1426);
                                }
                                break;
                            }
                        case "movielogos":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Logo, 400, 155);
                                }
                                break;
                            }
                        case "moviearts":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Art, 500, 281);
                                }
                                break;
                            }
                        case "moviethumbs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Thumb, 1000, 562);
                                }
                                break;
                            }
                        case "moviebanners":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Banner, 1000, 185);
                                }
                                break;
                            }
                        case "moviebackgrounds":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Backdrop, 1920, 1080);
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

        private void PopulateImageCategory(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken, ImageType type, int width, int height)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "hdmovielogo":
                        case "moviedisc":
                        case "hdmovieclearart":
                        case "movieposter":
                        case "movielogo":
                        case "movieart":
                        case "moviethumb":
                        case "moviebanner":
                        case "moviebackground":
                            {
                                var url = reader.GetAttribute("url");

                                if (!string.IsNullOrEmpty(url))
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

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            var id = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(id))
            {
                // Process images
                var xmlPath = FanartMovieProvider.Current.GetFanartXmlPath(id);

                var fileInfo = new FileInfo(xmlPath);

                return fileInfo.Exists && _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
            }

            return false;
        }
    }
}
