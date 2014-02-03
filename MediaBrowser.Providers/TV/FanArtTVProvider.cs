using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using System.Net;
using MediaBrowser.Providers.Music;

namespace MediaBrowser.Providers.TV
{
    class FanArtTvProvider : BaseMetadataProvider
    {
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/series/{0}/{1}/xml/all/1/1";

        internal static FanArtTvProvider Current { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        public FanArtTvProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            Current = this;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tvdb)))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var id = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(id))
            {
                // Process images
                var xmlPath = GetFanartXmlPath(id);

                var fileInfo = new FileInfo(xmlPath);

                return !fileInfo.Exists || _fileSystem.GetLastWriteTimeUtc(fileInfo) > providerInfo.LastRefreshed;
            }

            return base.NeedsRefreshBasedOnCompareDate(item, providerInfo);
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "1";
            }
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-tv");

            return dataPath;
        }

        public string GetFanartXmlPath(string tvdbId)
        {
            var dataPath = GetSeriesDataPath(ConfigurationManager.ApplicationPaths, tvdbId);
            return Path.Combine(dataPath, "fanart.xml");
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                var xmlPath = GetFanartXmlPath(seriesId);

                // Only download the xml if it doesn't already exist. The prescan task will take care of getting updates
                if (!File.Exists(xmlPath))
                {
                    await DownloadSeriesXml(seriesId, cancellationToken).ConfigureAwait(false);
                }

                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualFanartSeriesImageProvider.ProviderName).ConfigureAwait(false);

                await FetchFromXml(item, images.ToList(), cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);

            return true;
        }

        /// <summary>
        /// Fetches from XML.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchFromXml(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            var options = ConfigurationManager.Configuration.GetMetadataOptions("Series") ?? new MetadataOptions();

            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (options.IsEnabled(ImageType.Primary) && !item.HasImage(ImageType.Primary))
                {
                    await SaveImage(item, images, ImageType.Primary, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (options.IsEnabled(ImageType.Logo) && !item.HasImage(ImageType.Logo))
                {
                    await SaveImage(item, images, ImageType.Logo, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (options.IsEnabled(ImageType.Art) && !item.HasImage(ImageType.Art))
                {
                    await SaveImage(item, images, ImageType.Art, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (options.IsEnabled(ImageType.Thumb) && !item.HasImage(ImageType.Thumb))
                {
                    await SaveImage(item, images, ImageType.Thumb, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (options.IsEnabled(ImageType.Banner) && !item.HasImage(ImageType.Banner))
                {
                    await SaveImage(item, images, ImageType.Banner, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!item.LockedFields.Contains(MetadataFields.Backdrops))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var backdropLimit = options.GetLimit(ImageType.Backdrop);
                if (options.IsEnabled(ImageType.Backdrop) &&
                    item.BackdropImagePaths.Count < backdropLimit)
                {
                    foreach (var image in images.Where(i => i.Type == ImageType.Backdrop))
                    {
                        await _providerManager.SaveImage(item, image.Url, FanartArtistProvider.FanArtResourcePool, ImageType.Backdrop, null, cancellationToken)
                            .ConfigureAwait(false);

                        if (item.BackdropImagePaths.Count >= backdropLimit) break;
                    }
                }
            }
        }

        private async Task SaveImage(BaseItem item, List<RemoteImageInfo> images, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                try
                {
                    await _providerManager.SaveImage(item, image.Url, FanartArtistProvider.FanArtResourcePool, type, null, cancellationToken).ConfigureAwait(false);
                    break;
                }
                catch (HttpException ex)
                {
                    // Sometimes fanart has bad url's in their xml
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    break;
                }
            }
        }

        internal Task EnsureSeriesXml(string tvdbId, CancellationToken cancellationToken)
        {
            var xmlPath = GetSeriesDataPath(ConfigurationManager.ApplicationPaths, tvdbId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists)
            {
                if (ConfigurationManager.Configuration.EnableFanArtUpdates || (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return Task.FromResult(true);
                }
            }

            return DownloadSeriesXml(tvdbId, cancellationToken);
        }

        /// <summary>
        /// Downloads the series XML.
        /// </summary>
        /// <param name="tvdbId">The TVDB id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadSeriesXml(string tvdbId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, FanartArtistProvider.ApiKey, tvdbId);

            var xmlPath = GetFanartXmlPath(tvdbId);

            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

            using (var response = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanartArtistProvider.FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

    }
}
