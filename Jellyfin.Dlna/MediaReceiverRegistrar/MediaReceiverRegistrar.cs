using Jellyfin.Dlna.Service;
using Jellyfin.Common.Net;
using Jellyfin.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Dlna.MediaReceiverRegistrar
{
    public class MediaReceiverRegistrar : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        public MediaReceiverRegistrar(ILogger logger, IHttpClient httpClient, IServerConfigurationManager config)
            : base(logger, httpClient)
        {
            _config = config;
        }

        public string GetServiceXml()
        {
            return new MediaReceiverRegistrarXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            return new ControlHandler(
                _config,
                Logger)
                .ProcessControlRequest(request);
        }
    }
}
