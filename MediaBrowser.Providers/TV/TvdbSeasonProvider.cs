using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
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
    /// <summary>
    /// Class RemoteSeasonProvider
    /// </summary>
    class TvdbSeasonProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbSeasonProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public TvdbSeasonProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
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
            return item is Season;
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
                return "2";
            }
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var season = (Season)item;
            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(TvdbSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(imagesFileInfo) > providerInfo.LastRefreshed;
                }
            }
            return false;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Banner) && item.BackdropImagePaths.Count > 0)
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
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualTvdbSeasonImageProvider.ProviderName).ConfigureAwait(false);

            const int backdropLimit = 1;

            await DownloadImages(item, images.ToList(), backdropLimit, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        private async Task DownloadImages(BaseItem item, List<RemoteImageInfo> images, int backdropLimit, CancellationToken cancellationToken)
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

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Banner && !item.HasImage(ImageType.Banner))
            {
                var image = images.FirstOrDefault(i => i.Type == ImageType.Banner);

                if (image != null)
                {
                    await _providerManager.SaveImage(item, image.Url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Banner, null, cancellationToken)
                      .ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Backdrops && item.BackdropImagePaths.Count < backdropLimit)
            {
                var bdNo = item.BackdropImagePaths.Count;

                foreach (var backdrop in images.Where(i => i.Type == ImageType.Backdrop))
                {
                    var url = backdrop.Url;

                    if (item.ContainsImageWithSourceUrl(url))
                    {
                        continue;
                    }

                    await _providerManager.SaveImage(item, url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Backdrop, bdNo, cancellationToken).ConfigureAwait(false);

                    bdNo++;

                    if (item.BackdropImagePaths.Count >= backdropLimit) break;
                }
            }
        }
    }
}
