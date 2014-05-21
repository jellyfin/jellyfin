using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;

namespace MediaBrowser.Dlna.ConnectionManager
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public ConnectionManager(IDlnaManager dlna, ILogManager logManager, IServerConfigurationManager config)
        {
            _dlna = dlna;
            _config = config;
            _logger = logManager.GetLogger("UpnpConnectionManager");
        }

        public string GetServiceXml(IDictionary<string, string> headers)
        {
            return new ConnectionManagerXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                         _dlna.GetDefaultProfile();

            return new ControlHandler(_logger, profile, _config).ProcessControlRequest(request);
        }
    }
}
