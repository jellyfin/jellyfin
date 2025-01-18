#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.StudioImages
{
    /// <summary>
    /// Studio image provider.
    /// </summary>
    public class StudiosImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudiosImageProvider"/> class.
        /// </summary>
        /// <param name="config">The <see cref="IServerConfigurationManager"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
        public StudiosImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        public string Name => "Artwork Repository";

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Studio;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return [ImageType.Thumb];
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var thumbsPath = Path.Combine(_config.ApplicationPaths.CachePath, "imagesbyname", "remotestudiothumbs.txt");

            await EnsureThumbsList(thumbsPath, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var imageInfo = GetImage(item, thumbsPath, ImageType.Thumb, "thumb");

            if (imageInfo is null)
            {
                return [];
            }

            return [imageInfo];
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

        private string GetUrl(string image, string filename)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}/images/{1}/{2}.jpg", GetRepositoryUrl(), image, filename);
        }

        private async Task EnsureThumbsList(string file, CancellationToken cancellationToken)
        {
            string url = string.Format(CultureInfo.InvariantCulture, "{0}/thumbs.txt", GetRepositoryUrl());

            await EnsureList(url, file, _fileSystem, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return httpClient.GetAsync(url, cancellationToken);
        }

        /// <summary>
        /// Ensures the existence of a file listing.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="file">The file.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task to ensure existence of a file listing.</returns>
        public async Task EnsureList(string url, string file, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var fileInfo = fileSystem.GetFileInfo(file);

            if (!fileInfo.Exists || (DateTime.UtcNow - fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 1)
            {
                var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

                Directory.CreateDirectory(Path.GetDirectoryName(file));
                var response = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
                await using (response.ConfigureAwait(false))
                {
                    var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        await response.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Get matching image for an item.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="images">The enumerable of image strings.</param>
        /// <returns>The matching image string.</returns>
        public string FindMatch(BaseItem item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("&", string.Empty, StringComparison.Ordinal)
                .Replace("!", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get available image strings for a file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>All images strings of a file.</returns>
        public IEnumerable<string> GetAvailableImages(string file)
        {
            using var fileStream = File.OpenRead(file);
            using var reader = new StreamReader(fileStream);

            foreach (var line in reader.ReadAllLines())
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return line;
                }
            }
        }

        private string GetRepositoryUrl()
            => Plugin.Instance.Configuration.RepositoryUrl;
    }
}
