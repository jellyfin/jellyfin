using System.Net.Http;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="MediaReceiverRegistrarService" />.
    /// </summary>
    public class MediaReceiverRegistrarService : BaseService, IMediaReceiverRegistrar
    {
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaReceiverRegistrarService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{MediaReceiverRegistrarService}"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        public MediaReceiverRegistrarService(
            ILogger<MediaReceiverRegistrarService> logger,
            IHttpClientFactory httpClientFactory,
            IServerConfigurationManager config)
            : base(logger, httpClientFactory)
        {
            _config = config;
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return MediaReceiverRegistrarXmlBuilder.GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            return new ControlHandler(
                _config,
                Logger)
                .ProcessControlRequestAsync(request);
        }
    }
}
