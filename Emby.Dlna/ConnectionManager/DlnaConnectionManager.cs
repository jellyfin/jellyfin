#pragma warning disable CS1591
#nullable enable
using System;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.ConnectionManager
{
    public class DlnaConnectionManager : BaseService, IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly IServerConfigurationManager _configurationManager;

        public DlnaConnectionManager(
            ILogger logger,
            IServerConfigurationManager configurationManager,
            IHttpClient httpClient,
            IDlnaManager dlna)
            : base(logger, httpClient)
        {
            _dlna = dlna ?? throw new NullReferenceException(nameof(dlna));
            _configurationManager = configurationManager ?? throw new NullReferenceException(nameof(configurationManager));
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return ConnectionManagerXmlBuilder.GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = _dlna.GetProfile(request.Headers) ?? _dlna.GetDefaultProfile();

            return new DlnaControlHandler(_configurationManager, Logger, profile).ProcessControlRequestAsync(request);
        }
    }
}
