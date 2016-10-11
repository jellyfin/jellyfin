using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.ImagesByName;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Genres
{
    public class GenreImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        private readonly SemaphoreSlim _listResourcePool = new SemaphoreSlim(1, 1);

        public GenreImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
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
            get { return "Emby Designs"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Genre;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Thumb
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            return GetImages(item, true, true, cancellationToken);
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, bool posters, bool thumbs, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (posters)
            {
                var posterPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotegenreposters.txt");

                await EnsurePosterList(posterPath, cancellationToken).ConfigureAwait(false);

                list.Add(GetImage(item, posterPath, ImageType.Primary, "folder"));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (thumbs)
            {
                var thumbsPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotegenrethumbs.txt");

                await EnsureThumbsList(thumbsPath, cancellationToken).ConfigureAwait(false);

                list.Add(GetImage(item, thumbsPath, ImageType.Thumb, "thumb"));
            }

            return list.Where(i => i != null);
        }

        private RemoteImageInfo GetImage(IHasImages item, string filename, ImageType type, string remoteFilename)
        {
            var list = ImageUtils.GetAvailableImages(filename);

            var match = ImageUtils.FindMatch(item, list);

            if (!string.IsNullOrEmpty(match))
            {
                var url = GetUrl(match, remoteFilename);

                return new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = type,
                    Url = url
                };
            }

            return null;
        }

        private string GetUrl(string image, string filename)
        {
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/{0}/{1}.jpg", image, filename);
        }

        private Task EnsureThumbsList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genrethumbs.txt";

            return ImageUtils.EnsureList(url, file, _httpClient, _fileSystem, _listResourcePool, cancellationToken);
        }

        private Task EnsurePosterList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genreposters.txt";

            return ImageUtils.EnsureList(url, file, _httpClient, _fileSystem, _listResourcePool, cancellationToken);
        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                BufferContent = false
            });
        }
    }
}
