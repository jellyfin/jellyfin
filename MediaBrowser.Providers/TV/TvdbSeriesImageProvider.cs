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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeriesImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbSeriesImageProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public TvdbSeriesImageProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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
            // Run after fanart
            get { return MetadataProviderPriority.Fourth; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
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

        protected override DateTime CompareDate(BaseItem item)
        {
            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(TvdbSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(imagesFileInfo);
                }
            }

            return base.CompareDate(item);
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            var options = ConfigurationManager.Configuration.GetMetadataOptions("Series") ?? new MetadataOptions();
            
            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Banner) && item.BackdropImagePaths.Count >= options.GetLimit(ImageType.Backdrop))
            {
                return false;
            }
            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualTvdbSeriesImageProvider.ProviderName).ConfigureAwait(false);

            const int backdropLimit = 1;

            await DownloadImages(item, images.ToList(), backdropLimit, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private async Task DownloadImages(BaseItem item, List<RemoteImageInfo> images, int backdropLimit, CancellationToken cancellationToken)
        {
            var options = ConfigurationManager.Configuration.GetMetadataOptions("Series") ?? new MetadataOptions();
            
            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                if (!item.HasImage(ImageType.Primary))
                {
                    var image = images.FirstOrDefault(i => i.Type == ImageType.Primary);

                    if (image != null)
                    {
                        await _providerManager.SaveImage(item, image.Url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                if (options.IsEnabled(ImageType.Banner) && !item.HasImage(ImageType.Banner))
                {
                    var image = images.FirstOrDefault(i => i.Type == ImageType.Banner);

                    if (image != null)
                    {
                        await _providerManager.SaveImage(item, image.Url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Banner, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            if (options.IsEnabled(ImageType.Backdrop) && item.BackdropImagePaths.Count < backdropLimit && !item.LockedFields.Contains(MetadataFields.Backdrops))
            {
                foreach (var backdrop in images.Where(i => i.Type == ImageType.Backdrop && 
                    (!i.Width.HasValue || 
                    i.Width.Value >= options.GetMinWidth(ImageType.Backdrop))))
                {
                    var url = backdrop.Url;

                    await _providerManager.SaveImage(item, url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Backdrop, null, cancellationToken).ConfigureAwait(false);

                    if (item.BackdropImagePaths.Count >= backdropLimit) break;
                }
            }
        }
    }
}
