#nullable enable
using System;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class DlnaMediaReceiverRegistrar : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        public DlnaMediaReceiverRegistrar(
            ILoggerFactory loggerFactory,
            IHttpClient httpClient,
            IServerConfigurationManager config)
            : base(loggerFactory?.CreateLogger<DlnaMediaReceiverRegistrar>(), httpClient)
        {
            _config = config ?? throw new NullReferenceException(nameof(config));
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return new MediaReceiverRegistrarXmlBuilder().GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            return new ControlHandler(_config, _logger).ProcessControlRequestAsync(request);
        }
    }
}
