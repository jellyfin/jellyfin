#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbAlbumImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        public AudioDbAlbumImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 2;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Disc;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await AudioDbAlbumProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbAlbumProvider.GetAlbumInfoPath(_config.ApplicationPaths, id);

                FileStream jsonStream = AsyncFile.OpenRead(path);
                await using (jsonStream.ConfigureAwait(false))
                {
                    var obj = await JsonSerializer.DeserializeAsync<AudioDbAlbumProvider.RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                    if (obj is not null && obj.album is not null && obj.album.Count > 0)
                    {
                        return GetImages(obj.album[0]);
                    }
                }
            }

            return Enumerable.Empty<RemoteImageInfo>();
        }

        private List<RemoteImageInfo> GetImages(AudioDbAlbumProvider.Album item)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(item.strAlbumThumb))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strAlbumThumb,
                    Type = ImageType.Primary
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strAlbumCDart))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strAlbumCDart,
                    Type = ImageType.Disc
                });
            }

            return list;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return httpClient.GetAsync(url, cancellationToken);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is MusicAlbum;
    }
}
