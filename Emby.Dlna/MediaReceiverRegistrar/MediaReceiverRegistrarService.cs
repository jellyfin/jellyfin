#nullable enable
using System;
using System.Threading.Tasks;
using Emby.Dlna.Server;
using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Implements the DLNA functionality within JellyFin.
    /// </summary>
    public class MediaReceiverRegistrarService : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaReceiverRegistrarService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="config">Configuration instance.</param>
        /// <param name="httpClient">httpClient instance.</param>
        public MediaReceiverRegistrarService(
            ILogger logger,
            IServerConfigurationManager config,
            IHttpClient httpClient)
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
            return new ControlHandler(_config, Logger).ProcessControlRequestAsync(request);
        }
    }
}
