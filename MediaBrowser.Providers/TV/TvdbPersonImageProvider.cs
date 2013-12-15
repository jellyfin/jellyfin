using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class TvdbPersonImageProvider : BaseMetadataProvider
    {
        private readonly IProviderManager _providerManager;

        public TvdbPersonImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "2";
            }
        }

        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        public override bool Supports(BaseItem item)
        {
            return item is Person;
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
            if (string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualTvdbPersonImageProvider.ProviderName).ConfigureAwait(false);

                await DownloadImages(item, images.ToList(), cancellationToken).ConfigureAwait(false);

                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                return true;
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private async Task DownloadImages(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            if (!item.HasImage(ImageType.Primary) && !item.LockedFields.Contains(MetadataFields.Images))
            {
                var image = images.FirstOrDefault(i => i.Type == ImageType.Primary);

                if (image != null)
                {
                    await _providerManager.SaveImage(item, image.Url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken)
                      .ConfigureAwait(false);
                }
            }
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Fourth; }
        }
    }
}
