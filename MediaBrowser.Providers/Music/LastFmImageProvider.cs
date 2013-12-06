using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class LastFmArtistImageProvider
    /// </summary>
    public class LastFmImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastFmImageProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public LastFmImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager) : 
            base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist || item is MusicAlbum;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.HasImage(ImageType.Primary))
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
            if (!item.HasImage(ImageType.Primary))
            {
                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualLastFmImageProvider.ProviderName).ConfigureAwait(false);

                await DownloadImages(item, images.ToList(), cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);

            return true;
        }

        private async Task DownloadImages(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var configSetting = item is MusicAlbum
                ? ConfigurationManager.Configuration.DownloadMusicAlbumImages
                : ConfigurationManager.Configuration.DownloadMusicArtistImages;

            if (configSetting.Primary && !item.HasImage(ImageType.Primary))
            {
                var image = images.FirstOrDefault(i => i.Type == ImageType.Primary);

                if (image != null)
                {
                    await _providerManager.SaveImage(item, image.Url, LastfmBaseProvider.LastfmResourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Fifth; }
        }
    }
}
