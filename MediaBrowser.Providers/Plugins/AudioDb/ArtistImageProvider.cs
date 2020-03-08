using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbArtistImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public AudioDbArtistImageProvider(IServerConfigurationManager config, IJsonSerializer json, IHttpClient httpClient)
        {
            _config = config;
            _json = json;
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After fanart
        public int Order => 1;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Logo,
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await AudioDbArtistProvider.Current.EnsureArtistInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbArtistProvider.GetArtistInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<AudioDbArtistProvider.RootObject>(path);

                if (obj != null && obj.artists != null && obj.artists.Count > 0)
                {
                    return GetImages(obj.artists[0]);
                }
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(AudioDbArtistProvider.Artist item)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(item.strArtistThumb))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistThumb,
                    Type = ImageType.Primary
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strArtistLogo))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistLogo,
                    Type = ImageType.Logo
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strArtistBanner))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistBanner,
                    Type = ImageType.Banner
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strArtistFanart))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistFanart,
                    Type = ImageType.Backdrop
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strArtistFanart2))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistFanart2,
                    Type = ImageType.Backdrop
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strArtistFanart3))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strArtistFanart3,
                    Type = ImageType.Backdrop
                });
            }

            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => Plugin.Instance.Configuration.Enable && item is MusicArtist;
    }
}
