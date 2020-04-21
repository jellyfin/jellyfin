using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Syncplay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Syncplay
{
    [Route("/GetUtcTime", "GET", Summary = "Get UtcTime")]
    [Authenticated]
    public class GetUtcTime : IReturnVoid
    {
        // Nothing
    }

    /// <summary>
    /// Class TimeSyncService.
    /// </summary>
    public class TimeSyncService : BaseApiService
    {
        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The session context.
        /// </summary>
        private readonly ISessionContext _sessionContext;

        public TimeSyncService(
            ILogger<TimeSyncService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ISessionManager sessionManager,
            ISessionContext sessionContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _sessionManager = sessionManager;
            _sessionContext = sessionContext;
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <value>The current UTC time response.</value>
        public UtcTimeResponse Get(GetUtcTime request)
        {
            // Important to keep the following line at the beginning
            var requestReceptionTime = DateTime.UtcNow.ToUniversalTime().ToString("o");

            var response = new UtcTimeResponse();
            response.RequestReceptionTime = requestReceptionTime;

            // Important to keep the following two lines at the end
            var responseTransmissionTime = DateTime.UtcNow.ToUniversalTime().ToString("o");
            response.ResponseTransmissionTime = responseTransmissionTime;

            // Implementing NTP on such a high level results in this useless 
            // information being sent. On the other hand it enables future additions.
            return response;
        }
    }
}
