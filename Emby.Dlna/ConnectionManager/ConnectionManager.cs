using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.ConnectionManager
{
    public class ConnectionManager : BaseService, IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public ConnectionManager(IDlnaManager dlna, IServerConfigurationManager config, ILogger logger, IHttpClient httpClient)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _config = config;
            _logger = logger;
        }

        public string GetServiceXml()
        {
            return new ConnectionManagerXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                         _dlna.GetDefaultProfile();

            return new ControlHandler(_config, _logger, profile).ProcessControlRequest(request);
        }
    }
}
