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
    public class AudioDbAlbumImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _json;

        public AudioDbAlbumImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory, IJsonSerializer json)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _json = json;
        }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 2;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Disc
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await AudioDbAlbumProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbAlbumProvider.GetAlbumInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<AudioDbAlbumProvider.RootObject>(path);

                if (obj != null && obj.Album != null && obj.Album.Count > 0)
                {
                    return GetImages(obj.Album[0]);
                }
            }

            return new List<RemoteImageInfo>();
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is MusicAlbum;

        private IEnumerable<RemoteImageInfo> GetImages(AudioDbAlbumProvider.Album item)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(item.AlbumThumb))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.AlbumThumb,
                    Type = ImageType.Primary
                });
            }

            if (!string.IsNullOrWhiteSpace(item.AlbumCDart))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.AlbumCDart,
                    Type = ImageType.Disc
                });
            }

            return list;
        }
    }
}
