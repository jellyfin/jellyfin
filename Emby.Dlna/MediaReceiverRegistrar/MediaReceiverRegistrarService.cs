#pragma warning disable CS1591

using System.Net.Http;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class MediaReceiverRegistrarService : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        public MediaReceiverRegistrarService(
            ILogger<MediaReceiverRegistrarService> logger,
            IHttpClientFactory httpClientFactory,
            IServerConfigurationManager config)
            : base(logger, httpClientFactory)
        {
            _config = config;
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return new MediaReceiverRegistrarXmlBuilder().GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            return new ControlHandler(
                _config,
                Logger)
                .ProcessControlRequestAsync(request);
        }
    }
}
