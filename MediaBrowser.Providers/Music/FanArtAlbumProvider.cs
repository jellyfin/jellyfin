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
    public class FanartAlbumProvider : IRemoteImageProvider, IHasChangeMonitor, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public FanartAlbumProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
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
            return item is MusicAlbum;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Disc
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var list = new List<RemoteImageInfo>();

            var artistMusicBrainzId = album.MusicArtist.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                await FanartArtistProvider.Current.EnsureMovieXml(artistMusicBrainzId, cancellationToken).ConfigureAwait(false);

                var artistXmlPath = FanartArtistProvider.GetArtistXmlPath(_config.CommonApplicationPaths, artistMusicBrainzId);

                var musicBrainzReleaseGroupId = album.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

                var musicBrainzId = album.GetProviderId(MetadataProviders.MusicBrainzAlbum);

                try
                {
                    AddImages(list, artistXmlPath, musicBrainzId, musicBrainzReleaseGroupId, cancellationToken);
                }
                catch (FileNotFoundException)
                {

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

        /// <summary>
        /// Adds the images.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="xmlPath">The XML path.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="releaseGroupId">The release group identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImages(List<RemoteImageInfo> list, string xmlPath, string releaseId, string releaseGroupId, CancellationToken cancellationToken)
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
                                            AddImagesFromMusicNode(list, releaseId, releaseGroupId, subReader, cancellationToken);
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
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="releaseGroupId">The release group identifier.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImagesFromMusicNode(List<RemoteImageInfo> list, string releaseId, string releaseGroupId, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "albums":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    AddImagesFromAlbumsNode(list, releaseId, releaseGroupId, subReader, cancellationToken);
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
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="releaseGroupId">The release group identifier.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImagesFromAlbumsNode(List<RemoteImageInfo> list, string releaseId, string releaseGroupId, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "album":
                            {
                                var id = reader.GetAttribute("id");

                                using (var subReader = reader.ReadSubtree())
                                {
                                    if (string.Equals(id, releaseId, StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(id, releaseGroupId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        AddImages(list, subReader, cancellationToken);
                                    }
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
        /// Adds the images.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImages(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "cdart":
                            {
                                AddImage(list, reader, ImageType.Disc, 1000, 1000);
                                break;
                            }
                        case "albumcover":
                            {
                                AddImage(list, reader, ImageType.Primary, 1000, 1000);
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

        public int Order
        {
            get
            {
                // After embedded provider
                return 1;
            }
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

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (!_config.Configuration.EnableFanArtUpdates)
            {
                return false;
            }

            var album = (MusicAlbum)item;
            var artist = album.MusicArtist;

            if (artist != null)
            {
                var artistMusicBrainzId = artist.GetProviderId(MetadataProviders.MusicBrainzArtist);

                if (!String.IsNullOrEmpty(artistMusicBrainzId))
                {
                    // Process images
                    var artistXmlPath = FanartArtistProvider.GetArtistXmlPath(_config.CommonApplicationPaths, artistMusicBrainzId);

                    var fileInfo = new FileInfo(artistXmlPath);

                    return !fileInfo.Exists || _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
                }
            }

            return false;
        }
    }
}
