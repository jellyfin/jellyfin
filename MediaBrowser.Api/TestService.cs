using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Service for testing path value.
    /// </summary>
    public class TestService : BaseApiService
    {
        /// <summary>
        /// Test service.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TestService}"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="httpResultFactory">Instance of the <see cref="IHttpResultFactory"/> interface.</param>
        public TestService(
            ILogger<TestService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
        }
    }
}
