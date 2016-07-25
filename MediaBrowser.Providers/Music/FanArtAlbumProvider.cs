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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Music
{
    public class FanartAlbumProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        public FanartAlbumProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
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

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var list = new List<RemoteImageInfo>();

            var musicArtist = album.MusicArtist;

            if (musicArtist == null)
            {
                return list;
            }

            var artistMusicBrainzId = musicArtist.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                await FanartArtistProvider.Current.EnsureArtistJson(artistMusicBrainzId, cancellationToken).ConfigureAwait(false);

                var artistJsonPath = FanartArtistProvider.GetArtistJsonPath(_config.CommonApplicationPaths, artistMusicBrainzId);

                var musicBrainzReleaseGroupId = album.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

                var musicBrainzId = album.GetProviderId(MetadataProviders.MusicBrainzAlbum);

                try
                {
                    AddImages(list, artistJsonPath, musicBrainzId, musicBrainzReleaseGroupId, cancellationToken);
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
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
        /// <param name="path">The path.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="releaseGroupId">The release group identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void AddImages(List<RemoteImageInfo> list, string path, string releaseId, string releaseGroupId, CancellationToken cancellationToken)
        {
            var obj = _jsonSerializer.DeserializeFromFile<FanartArtistProvider.FanartArtistResponse>(path);

            if (obj.albums != null)
            {
                var album = obj.albums.FirstOrDefault(i => string.Equals(i.release_group_id, releaseGroupId, StringComparison.OrdinalIgnoreCase));

                if (album != null)
                {
                    PopulateImages(list, album.albumcover, ImageType.Primary, 1000, 1000);
                    PopulateImages(list, album.cdart, ImageType.Disc, 1000, 1000);
                }
            }
        }

        private Regex _regex_http = new Regex("^http://");
        private void PopulateImages(List<RemoteImageInfo> list,
            List<FanartArtistProvider.FanartArtistImage> images,
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
                ResourcePool = FanartArtistProvider.Current.FanArtResourcePool
            });
        }
    }
}
