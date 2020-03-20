#pragma warning disable CS1591

using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class MediaReceiverRegistrar : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        public MediaReceiverRegistrar(
            ILogger<MediaReceiverRegistrar> logger,
            IHttpClient httpClient,
            IServerConfigurationManager config)
            : base(logger, httpClient)
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
