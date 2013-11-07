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
    public class ManualFanartArtistProvider : IImageProvider
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;

        public ManualFanartArtistProvider(IServerConfigurationManager config)
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
            return item is MusicArtist || item is Artist;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var artistMusicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                var artistXmlPath = FanArtArtistProvider.GetArtistDataPath(_config.CommonApplicationPaths, artistMusicBrainzId);
                artistXmlPath = Path.Combine(artistXmlPath, "fanart.xml");

                try
                {
                    AddImages(list, artistXmlPath, cancellationToken);
                }
                catch (FileNotFoundException)
                {

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
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list);
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

            if (!string.IsNullOrEmpty(size))
            {
                int sizeNum;
                if (int.TryParse(size, NumberStyles.Any, _usCulture, out sizeNum))
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

            if (!string.IsNullOrEmpty(likesString) && int.TryParse(likesString, NumberStyles.Any, _usCulture, out likes))
            {
                info.CommunityRating = likes;
            }

            list.Add(info);
        }

        public int Priority
        {
            get { return 1; }
        }
    }
}
