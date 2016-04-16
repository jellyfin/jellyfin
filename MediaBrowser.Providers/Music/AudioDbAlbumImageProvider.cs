using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class AudioDbAlbumImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public AudioDbAlbumImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IJsonSerializer json)
        {
            _config = config;
            _httpClient = httpClient;
            _json = json;
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
            var id = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await AudioDbAlbumProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbAlbumProvider.GetAlbumInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<AudioDbAlbumProvider.RootObject>(path);

                if (obj != null && obj.album != null && obj.album.Count > 0)
                {
                    return GetImages(obj.album[0]);
                }
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(AudioDbAlbumProvider.Album item)
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
        
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = AudioDbArtistProvider.Current.AudioDbResourcePool
            });
        }

        public string Name
        {
            get { return "TheAudioDB"; }
        }

        public int Order
        {
            get
            {
                // After embedded and fanart
                return 2;
            }
        }

        public bool Supports(IHasImages item)
        {
            return item is MusicAlbum;
        }
    }
}
