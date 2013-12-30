using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.ImagesByName
{
    public class StudioImageProvider : BaseMetadataProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(5, 5);

        public StudioImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Studio;
        }

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

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Thumb))
            {
                return false;
            }

            // Try again periodically in case new images were added
            if ((DateTime.UtcNow - providerInfo.LastRefreshed).TotalDays > 14)
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
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
                return "6";
            }
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Thumb))
            {
                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                return true;
            }

            var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, StudiosManualImageProvider.ProviderName).ConfigureAwait(false);

            await DownloadImages(item, images.ToList(), cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private async Task DownloadImages(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!item.HasImage(ImageType.Primary))
                {
                    await SaveImage(item, images, ImageType.Primary, cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (!item.HasImage(ImageType.Thumb))
                {
                    await SaveImage(item, images, ImageType.Thumb, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!item.LockedFields.Contains(MetadataFields.Backdrops))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.BackdropImagePaths.Count == 0)
                {
                    foreach (var image in images.Where(i => i.Type == ImageType.Backdrop))
                    {
                        await _providerManager.SaveImage(item, image.Url, _resourcePool, ImageType.Backdrop, null, cancellationToken)
                            .ConfigureAwait(false);

                        break;
                    }
                }
            }
        }


        private async Task SaveImage(BaseItem item, IEnumerable<RemoteImageInfo> images, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                try
                {
                    await _providerManager.SaveImage(item, image.Url, _resourcePool, type, null, cancellationToken).ConfigureAwait(false);
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

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }
    }
}
