using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
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
