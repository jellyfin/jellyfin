using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IFileSystem _fileSystem;
        private readonly IXmlReaderSettingsFactory _xmlReaderSettingsFactory;

        public TvdbSeriesImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _xmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "TheTVDB"; }
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
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            if (TvdbSeriesProvider.IsValidSeries(item.ProviderIds))
            {
                var language = item.GetPreferredMetadataLanguage();

                var seriesDataPath = await TvdbSeriesProvider.Current.EnsureSeriesInfo(item.ProviderIds, item.Name, item.ProductionYear, language, cancellationToken).ConfigureAwait(false);

                var path = Path.Combine(seriesDataPath, "banners.xml");

                try
                {
                    return GetImages(path, language, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // No tvdb data yet. Don't blow up
                }
                catch (IOException)
                {
                    // No tvdb data yet. Don't blow up
                }
            }

            return new RemoteImageInfo[] { };
        }

        private IEnumerable<RemoteImageInfo> GetImages(string xmlPath, string preferredLanguage, CancellationToken cancellationToken)
        {
            var settings = _xmlReaderSettingsFactory.Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;

            var list = new List<RemoteImageInfo>();

            using (var fileStream = _fileSystem.GetFileStream(xmlPath, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        reader.MoveToContent();
                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "Banner":
                                        {
                                            if (reader.IsEmptyElement)
                                            {
                                                reader.Read();
                                                continue;
                                            }
                                            using (var subtree = reader.ReadSubtree())
                                            {
                                                AddImage(subtree, list);
                                            }
                                            break;
                                        }
                                    default:
                                        reader.Skip();
                                        break;
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
            }

            var isLanguageEn = string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
            {
                if (string.Equals(preferredLanguage, i.Language, StringComparison.OrdinalIgnoreCase))
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
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();
        }

        private void AddImage(XmlReader reader, List<RemoteImageInfo> images)
        {
            reader.MoveToContent();

            string bannerType = null;
            string url = null;
            int? bannerSeason = null;
            int? width = null;
            int? height = null;
            string language = null;
            double? rating = null;
            int? voteCount = null;
            string thumbnailUrl = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Rating":
                            {
                                var val = reader.ReadElementContentAsString() ?? string.Empty;

                                double rval;

                                if (double.TryParse(val, NumberStyles.Any, _usCulture, out rval))
                                {
                                    rating = rval;
                                }

                                break;
                            }

                        case "RatingCount":
                            {
                                var val = reader.ReadElementContentAsString() ?? string.Empty;

                                int rval;

                                if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                {
                                    voteCount = rval;
                                }

                                break;
                            }

                        case "Language":
                            {
                                language = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "ThumbnailPath":
                            {
                                thumbnailUrl = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerType":
                            {
                                bannerType = reader.ReadElementContentAsString() ?? string.Empty;

                                break;
                            }

                        case "BannerPath":
                            {
                                url = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerType2":
                            {
                                var bannerType2 = reader.ReadElementContentAsString() ?? string.Empty;

                                // Sometimes the resolution is stuffed in here
                                var resolutionParts = bannerType2.Split('x');

                                if (resolutionParts.Length == 2)
                                {
                                    int rval;

                                    if (int.TryParse(resolutionParts[0], NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        width = rval;
                                    }

                                    if (int.TryParse(resolutionParts[1], NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        height = rval;
                                    }

                                }

                                break;
                            }

                        case "Season":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    bannerSeason = int.Parse(val);
                                }
                                break;
                            }


                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            if (!string.IsNullOrEmpty(url) && !bannerSeason.HasValue)
            {
                var imageInfo = new RemoteImageInfo
                {
                    RatingType = RatingType.Score,
                    CommunityRating = rating,
                    VoteCount = voteCount,
                    Url = TVUtils.BannerUrl + url,
                    ProviderName = Name,
                    Language = language,
                    Width = width,
                    Height = height
                };

                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    imageInfo.ThumbnailUrl = TVUtils.BannerUrl + thumbnailUrl;
                }

                if (string.Equals(bannerType, "poster", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Primary;
                    images.Add(imageInfo);
                }
                else if (string.Equals(bannerType, "series", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Banner;
                    images.Add(imageInfo);
                }
                else if (string.Equals(bannerType, "fanart", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Backdrop;
                    images.Add(imageInfo);
                }
            }

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
                Url = url
            });
        }
    }
}
