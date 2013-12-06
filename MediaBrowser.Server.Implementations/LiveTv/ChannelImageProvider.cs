using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class ChannelImageProvider : BaseMetadataProvider
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IProviderManager _providerManager;

        public ChannelImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, ILiveTvManager liveTvManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _liveTvManager = liveTvManager;
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Channel;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            return !item.HasImage(ImageType.Primary);
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (item.HasImage(ImageType.Primary))
            {
                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                return true;
            }

            try
            {
                await DownloadImage(item, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                // Don't fail the provider on a 404
                if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }


            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private async Task DownloadImage(BaseItem item, CancellationToken cancellationToken)
        {
            var channel = (Channel)item;

            var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, channel.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (service != null)
            {
                var response = await service.GetChannelImageAsync(channel.ChannelId, cancellationToken).ConfigureAwait(false);

                // Dummy up the original url
                var url = channel.ServiceName + channel.ChannelId;

                await _providerManager.SaveImage(channel, response.Stream, response.MimeType, ImageType.Primary, null, url, cancellationToken).ConfigureAwait(false);
            }
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }
    }
}
