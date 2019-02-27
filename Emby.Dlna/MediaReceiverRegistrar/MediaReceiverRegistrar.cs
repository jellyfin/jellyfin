using System.Collections.Generic;
using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class MediaReceiverRegistrar : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;
        protected readonly IXmlReaderSettingsFactory XmlReaderSettingsFactory;

        public MediaReceiverRegistrar(ILogger logger, IHttpClient httpClient, IServerConfigurationManager config, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(logger, httpClient)
        {
            _config = config;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        public string GetServiceXml()
        {
            return new MediaReceiverRegistrarXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            return new ControlHandler(
                _config,
                Logger, XmlReaderSettingsFactory)
                .ProcessControlRequest(request);
        }
    }
}
