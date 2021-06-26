using System;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ConfigurationExtensions = MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Redirect requests without baseurl prefix to the baseurl prefixed URL.
    /// </summary>
    public class BaseUrlRedirectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BaseUrlRedirectionMiddleware> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseUrlRedirectionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The application configuration.</param>
        public BaseUrlRedirectionMiddleware(
            RequestDelegate next,
            ILogger<BaseUrlRedirectionMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext, IServerConfigurationManager serverConfigurationManager)
        {
            var localPath = httpContext.Request.Path.ToString();
            var baseUrlPrefix = serverConfigurationManager.GetNetworkConfiguration().BaseUrl;

            if (!string.IsNullOrEmpty(baseUrlPrefix))
            {
                var startsWithBaseUrl = localPath.StartsWith(baseUrlPrefix, StringComparison.OrdinalIgnoreCase);

                if (!startsWithBaseUrl
                    && (localPath.Equals("/health", StringComparison.OrdinalIgnoreCase)
                        || localPath.Equals("/health/", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug("Redirecting /health check");
                    httpContext.Response.Redirect(baseUrlPrefix + "/health");
                    return;
                }

                if (!startsWithBaseUrl)
                {
                    // Always redirect back to the default path if the base prefix is invalid or missing
                    _logger.LogDebug("Normalizing an URL at {LocalPath}", localPath);
                    httpContext.Response.Redirect(baseUrlPrefix + "/" + _configuration[ConfigurationExtensions.DefaultRedirectKey]);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(localPath)
                     || localPath.Equals("/", StringComparison.Ordinal))
            {
                // Always redirect back to the default path if root is requested.
                _logger.LogDebug("Normalizing an URL at {LocalPath}", localPath);
                httpContext.Response.Redirect("/" + _configuration[ConfigurationExtensions.DefaultRedirectKey]);
                return;
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
