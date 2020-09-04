#pragma warning disable CS1591

using System.Net.Http;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.ConnectionManager
{
    public class ConnectionManagerService : BaseService, IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly IServerConfigurationManager _config;

        public ConnectionManagerService(
            IDlnaManager dlna,
            IServerConfigurationManager config,
            ILogger<ConnectionManagerService> logger,
            IHttpClientFactory httpClientFactory)
            : base(logger, httpClientFactory)
        {
            _dlna = dlna;
            _config = config;
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return new ConnectionManagerXmlBuilder().GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                         _dlna.GetDefaultProfile();

            return new ControlHandler(_config, Logger, profile).ProcessControlRequestAsync(request);
        }
    }
}
