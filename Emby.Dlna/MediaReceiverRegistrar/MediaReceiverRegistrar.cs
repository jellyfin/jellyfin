using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Emby.Dlna.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Xml;

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

        public string GetServiceXml(IDictionary<string, string> headers)
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
