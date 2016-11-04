using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Emby.Dlna.Service;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using MediaBrowser.Model.Xml;

namespace Emby.Dlna.ConnectionManager
{
    public class ConnectionManager : BaseService, IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        protected readonly IXmlReaderSettingsFactory XmlReaderSettingsFactory;

        public ConnectionManager(IDlnaManager dlna, IServerConfigurationManager config, ILogger logger, IHttpClient httpClient, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _config = config;
            _logger = logger;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        public string GetServiceXml(IDictionary<string, string> headers)
        {
            return new ConnectionManagerXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                         _dlna.GetDefaultProfile();

            return new ControlHandler(_config, _logger, XmlReaderSettingsFactory, profile).ProcessControlRequest(request);
        }
    }
}
