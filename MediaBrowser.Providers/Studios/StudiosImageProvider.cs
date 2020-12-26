#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Studios
{
    public class StudiosImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        public StudiosImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
        }

        public static int Order => 0;

        public string Name => "Emby Designs";

        public static string FindMatch(BaseItem item, IEnumerable<string> images)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<string> GetAvailableImages(string file)
        {
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream);
            var lines = new List<string>();

            while (!reader.EndOfStream)
            {
                var text = reader.ReadLine();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text);
                }
            }

            return lines;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }

        /// <summary>
        /// Ensures the list.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="file">The file.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task<string> EnsureList(string url, string file, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            var fileInfo = fileSystem.GetFileInfo(file);

            if (!fileInfo.Exists || (DateTime.UtcNow - fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 1)
            {
                var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

                Directory.CreateDirectory(Path.GetDirectoryName(file));
                await using var response = await httpClient.GetStreamAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope: Wrongly identified
                await using var fileStream = new FileStream(file, FileMode.Create);
#pragma warning restore CA2000 // Dispose objects before losing scope
                await response.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            return file;
        }

        public bool Supports(BaseItem item)
        {
            return item is Studio;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            return GetImages(item, true, true, cancellationToken);
        }

        private static string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("&", string.Empty, StringComparison.Ordinal)
                .Replace("!", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal);
        }

        private static string GetUrl(string image, string filename)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studios/{0}/{1}.jpg", image, filename);
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, bool posters, bool thumbs, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (posters)
            {
                var posterPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotestudioposters.txt");

                posterPath = await EnsurePosterList(posterPath, cancellationToken).ConfigureAwait(false);

                list.Add(GetImage(item, posterPath, ImageType.Primary, "folder"));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (thumbs)
            {
                var thumbsPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotestudiothumbs.txt");

                thumbsPath = await EnsureThumbsList(thumbsPath, cancellationToken).ConfigureAwait(false);

                list.Add(GetImage(item, thumbsPath, ImageType.Thumb, "thumb"));
            }

            return list.Where(i => i != null);
        }

        private RemoteImageInfo GetImage(BaseItem item, string filename, ImageType type, string remoteFilename)
        {
            var list = GetAvailableImages(filename);

            var match = FindMatch(item, list);

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

        private Task<string> EnsureThumbsList(string file, CancellationToken cancellationToken)
        {
            const string Url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studiothumbs.txt";

            return EnsureList(Url, file, _fileSystem, cancellationToken);
        }

        private Task<string> EnsurePosterList(string file, CancellationToken cancellationToken)
        {
            const string Url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studioposters.txt";

            return EnsureList(Url, file, _fileSystem, cancellationToken);
        }
    }
}
