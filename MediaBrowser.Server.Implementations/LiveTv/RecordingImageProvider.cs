using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class RecordingImageProvider : BaseMetadataProvider
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;

        public RecordingImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, ILiveTvManager liveTvManager, IProviderManager providerManager, IFileSystem fileSystem, IHttpClient httpClient)
            : base(logManager, configurationManager)
        {
            _liveTvManager = liveTvManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public override bool Supports(BaseItem item)
        {
            return item is ILiveTvRecording;
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
                changed = await DownloadImage((ILiveTvRecording)item, cancellationToken).ConfigureAwait(false);
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

        private async Task<bool> DownloadImage(ILiveTvRecording item, CancellationToken cancellationToken)
        {
            var recordingInfo = item.RecordingInfo;

            Stream imageStream = null;
            string contentType = null;

            if (!string.IsNullOrEmpty(recordingInfo.ImagePath))
            {
                contentType = "image/" + Path.GetExtension(recordingInfo.ImagePath).ToLower();
                imageStream = _fileSystem.GetFileStream(recordingInfo.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true);
            }
            else if (!string.IsNullOrEmpty(recordingInfo.ImageUrl))
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = recordingInfo.ImageUrl
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
            else if (recordingInfo.HasImage ?? true)
            {
                var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, item.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (service != null)
                {
                    try
                    {
                        var response = await service.GetRecordingImageAsync(recordingInfo.Id, cancellationToken).ConfigureAwait(false);

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
                var url = item.ServiceName + recordingInfo.Id;

                await _providerManager.SaveImage((BaseItem)item, imageStream, contentType, ImageType.Primary, null, url, cancellationToken).ConfigureAwait(false);
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
