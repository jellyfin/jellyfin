#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.Http;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _json;

        public AudioDbArtistImageProvider(IServerConfigurationManager config, IJsonSerializer json, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _json = json;
            _httpClientFactory = httpClientFactory;
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
            var id = item.GetProviderId(MetadataProvider.MusicBrainzArtist);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await AudioDbArtistProvider.Current.EnsureArtistInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbArtistProvider.GetArtistInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<AudioDbArtistProvider.RootObject>(path);

                if (obj != null && obj.Artists != null && obj.Artists.Count > 0)
                {
                    return GetImages(obj.Artists[0]);
                }
            }

            return new List<RemoteImageInfo>();
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is MusicArtist;

        private IEnumerable<RemoteImageInfo> GetImages(AudioDbArtistProvider.Artist item)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(item.ArtistThumb))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistThumb,
                    Type = ImageType.Primary
                });
            }

            if (!string.IsNullOrWhiteSpace(item.ArtistLogo))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistLogo,
                    Type = ImageType.Logo
                });
            }

            if (!string.IsNullOrWhiteSpace(item.ArtistBanner))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistBanner,
                    Type = ImageType.Banner
                });
            }

            if (!string.IsNullOrWhiteSpace(item.ArtistFanart))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistFanart,
                    Type = ImageType.Backdrop
                });
            }

            if (!string.IsNullOrWhiteSpace(item.ArtistFanart2))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistFanart2,
                    Type = ImageType.Backdrop
                });
            }

            if (!string.IsNullOrWhiteSpace(item.ArtistFanart3))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.ArtistFanart3,
                    Type = ImageType.Backdrop
                });
            }

            return list;
        }
    }
}
