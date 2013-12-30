using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.ImagesByName
{
    public class StudiosManualImageProvider : IImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        private readonly SemaphoreSlim _listResourcePool = new SemaphoreSlim(1, 1);

        public StudiosManualImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
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
            get { return "Media Browser"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Studio;
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            return GetImages(item, imageType == ImageType.Primary, imageType == ImageType.Backdrop, cancellationToken);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            return GetImages(item, true, true, cancellationToken);
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, bool posters, bool backdrops, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (posters)
            {
                var posterPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotestudioposters.txt");

                await EnsurePosterList(posterPath, cancellationToken).ConfigureAwait(false);

                list.Add(GetImage(item, posterPath, ImageType.Primary, "folder"));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (backdrops)
            {
                var thumbsPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotestudiothumbs.txt");

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
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studios/{0}/{1}.jpg", image, filename);
        }

        private Task EnsureThumbsList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studiothumbs.txt";

            return ImageUtils.EnsureList(url, file, _httpClient, _fileSystem, _listResourcePool, cancellationToken);
        }

        private Task EnsurePosterList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studioposters.txt";

            return ImageUtils.EnsureList(url, file, _httpClient, _fileSystem, _listResourcePool, cancellationToken);
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
