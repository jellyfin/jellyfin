using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.SyncPlay
{
    [Route("/GetUtcTime", "GET", Summary = "Get UtcTime")]
    public class GetUtcTime : IReturnVoid
    {
        // Nothing
    }

    /// <summary>
    /// Class TimeSyncService.
    /// </summary>
    public class TimeSyncService : BaseApiService
    {
        public TimeSyncService(
            ILogger<TimeSyncService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            // Do nothing
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
