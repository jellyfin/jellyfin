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

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaMediaReceiverRegistrar"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="httpClient">httpClient instance.</param>
        /// <param name="config">Configuration instance</param>
        public DlnaMediaReceiverRegistrar(
            ILogger logger,
            IHttpClient httpClient,
            IServerConfigurationManager config)
            : base(logger, httpClient)
        {
            _config = config ?? throw new NullReferenceException(nameof(config));
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return MediaReceiverRegistrarXmlBuilder.GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            return new ControlHandler(_config, _logger).ProcessControlRequestAsync(request);
        }
    }
}
