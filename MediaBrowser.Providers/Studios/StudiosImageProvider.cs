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
using MediaBrowser.Model.IO;
using System;
using MediaBrowser.Common.Progress;

namespace MediaBrowser.Providers.Studios
{
    public class StudiosImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public StudiosImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Emby Designs"; }
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
            var list = GetAvailableImages(filename, _fileSystem);

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

        private string GetUrl(string image, string filename)
        {
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studios/{0}/{1}.jpg", image, filename);
        }

        private Task<string> EnsureThumbsList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studiothumbs.txt";

            return EnsureList(url, file, _httpClient, _fileSystem, cancellationToken);
        }

        private Task<string> EnsurePosterList(string file, CancellationToken cancellationToken)
        {
            const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studioposters.txt";

            return EnsureList(url, file, _httpClient, _fileSystem, cancellationToken);
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

        /// <summary>
        /// Ensures the list.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="file">The file.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task<string> EnsureList(string url, string file, IHttpClient httpClient, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var fileInfo = fileSystem.GetFileInfo(file);

            if (!fileInfo.Exists || (DateTime.UtcNow - fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 1)
            {
                var temp = await httpClient.GetTempFile(new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Progress = new SimpleProgress<double>(),
                    Url = url

                }).ConfigureAwait(false);

                fileSystem.CreateDirectory(fileSystem.GetDirectoryName(file));

                try
                {
                    fileSystem.CopyFile(temp, file, true);
                }
                catch
                {

                }

                return temp;
            }

            return file;
        }

        public string FindMatch(BaseItem item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace("&", string.Empty)
                .Replace("!", string.Empty)
                .Replace(",", string.Empty)
                .Replace("/", string.Empty);
        }

        public IEnumerable<string> GetAvailableImages(string file, IFileSystem fileSystem)
        {
            using (var fileStream = fileSystem.GetFileStream(file, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
            {
                using (var reader = new StreamReader(fileStream))
                {
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
            }
        }

    }
}
