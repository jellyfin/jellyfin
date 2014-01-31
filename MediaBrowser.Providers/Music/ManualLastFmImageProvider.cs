using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class ManualLastFmImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;

        public ManualLastFmImageProvider(IHttpClient httpClient, IServerConfigurationManager config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "last.fm"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is MusicAlbum || item is MusicArtist;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            RemoteImageInfo info = null;

            var musicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            var album = item as MusicAlbum;
            if (album != null)
            {
                info = GetInfo(album.LastFmImageUrl, album.LastFmImageSize);
            }

            var musicArtist = item as MusicArtist;
            if (musicArtist != null && !string.IsNullOrEmpty(musicBrainzId))
            {
                var cachePath = Path.Combine(_config.ApplicationPaths.CachePath, "lastfm", musicBrainzId, "image.txt");

                try
                {
                    var parts = File.ReadAllText(cachePath).Split('|');

                    info = GetInfo(parts.FirstOrDefault(), parts.LastOrDefault());
                }
                catch (DirectoryNotFoundException ex)
                {
                }
                catch (FileNotFoundException ex)
                {
                }
            
            }

            if (info != null)
            {
                list.Add(info);
            }

            // The only info we have is size
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list.OrderByDescending(i => i.Width ?? 0));
        }

        private RemoteImageInfo GetInfo(string url, string size)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var info = new RemoteImageInfo
            {
                ProviderName = Name,
                Url = url,
                Type = ImageType.Primary
            };

            if (string.Equals(size, "mega", StringComparison.OrdinalIgnoreCase))
            {
                
            }
            else if (string.Equals(size, "extralarge", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "large", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "medium", StringComparison.OrdinalIgnoreCase))
            {

            }

            return info;
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
                ResourcePool = LastFmArtistProvider.LastfmResourcePool
            });
        }
    }
}
