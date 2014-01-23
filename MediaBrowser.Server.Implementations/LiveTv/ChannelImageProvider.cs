using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
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
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;

        public ChannelImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, ILiveTvManager liveTvManager, IProviderManager providerManager, IFileSystem fileSystem, IHttpClient httpClient)
            : base(logManager, configurationManager)
        {
            _liveTvManager = liveTvManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public override bool Supports(BaseItem item)
        {
            return item is LiveTvChannel;
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

            var changed = true;

            try
            {
                changed = await DownloadImage((LiveTvChannel)item, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                // Don't fail the provider on a 404
                if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            if (changed)
            {
                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            }

            return changed;
        }

        private async Task<bool> DownloadImage(LiveTvChannel item, CancellationToken cancellationToken)
        {
            Stream imageStream = null;
            string contentType = null;

            if (!string.IsNullOrEmpty(item.ProviderImagePath))
            {
                contentType = "image/" + Path.GetExtension(item.ProviderImagePath).ToLower();
                imageStream = _fileSystem.GetFileStream(item.ProviderImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true);
            }
            else if (!string.IsNullOrEmpty(item.ProviderImageUrl))
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = item.ProviderImageUrl
                };

                var response = await _httpClient.GetResponse(options).ConfigureAwait(false);

                if (!response.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error("Provider did not return an image content type.");
                    return false;
                }

                imageStream = response.Content;
                contentType = response.ContentType;
            }
            else if (item.HasProviderImage ?? true)
            {
                var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, item.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (service != null)
                {
                    try
                    {
                        var response = await service.GetChannelImageAsync(item.ExternalId, cancellationToken).ConfigureAwait(false);

                        if (response != null)
                        {
                            imageStream = response.Stream;
                            contentType = response.MimeType;
                        }
                    }
                    catch (NotImplementedException)
                    {
                        return false;
                    }
                }
            }

            if (imageStream != null)
            {
                // Dummy up the original url
                var url = item.ServiceName + item.ExternalId;

                await _providerManager.SaveImage(item, imageStream, contentType, ImageType.Primary, null, url, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
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
