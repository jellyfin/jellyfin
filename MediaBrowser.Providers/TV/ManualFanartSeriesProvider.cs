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
    public class ManualFanartSeriesImageProvider : IImageProvider
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;

        public ManualFanartSeriesImageProvider(IServerConfigurationManager config)
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

        public bool Supports(IHasImages item)
        {
            return item is Series;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var series = (Series)item;

            var id = series.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(id))
            {
                var xmlPath = FanArtTvProvider.Current.GetFanartXmlPath(id);

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
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list);
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

        public int Priority
        {
            get { return 0; }
        }
    }
}
