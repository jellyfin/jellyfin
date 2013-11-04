using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
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

namespace MediaBrowser.Providers.TV
{
    public class ManualFanartSeasonImageProvider : IImageProvider
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;

        public ManualFanartSeasonImageProvider(IServerConfigurationManager config)
        {
            _config = config;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "FanArt"; }
        }

        public bool Supports(BaseItem item)
        {
            return item is Season;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var series = ((Season)item).Series;

            if (series != null)
            {
                var id = series.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(id) && item.IndexNumber.HasValue)
                {
                    var xmlPath = FanArtTvProvider.Current.GetFanartXmlPath(id);

                    try
                    {
                        AddImages(list, item.IndexNumber.Value, xmlPath, cancellationToken);
                    }
                    catch (FileNotFoundException)
                    {
                        // No biggie. Don't blow up
                    }
                }
            }

            var language = _config.Configuration.PreferredMetadataLanguage;

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            // Sort first by width to prioritize HD versions
            list = list.OrderByDescending(i => i.Width ?? 0)
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
                .ToList();

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list);
        }

        private void AddImages(List<RemoteImageInfo> list, int seasonNumber, string xmlPath, CancellationToken cancellationToken)
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
                                            AddImages(list, subReader, seasonNumber, cancellationToken);
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

        private void AddImages(List<RemoteImageInfo> list, XmlReader reader, int seasonNumber, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "seasonthumbs":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Thumb, 500, 281, seasonNumber);
                                }
                                break;
                            }
                        case "showbackgrounds":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Backdrop, 1920, 1080, seasonNumber);
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

        private void PopulateImageCategory(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken, ImageType type, int width, int height, int seasonNumber)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "seasonthumb":
                        case "showbackground":
                            {
                                var url = reader.GetAttribute("url");
                                var season = reader.GetAttribute("season");

                                int imageSeasonNumber;

                                if (!string.IsNullOrEmpty(url) &&
                                    !string.IsNullOrEmpty(season) &&
                                    int.TryParse(season, NumberStyles.Any, _usCulture, out imageSeasonNumber) &&
                                    seasonNumber == imageSeasonNumber)
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

        public int Priority
        {
            get { return 0; }
        }
    }
}
