using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Dynamic cors middleware.
    /// </summary>
    public class DynamicCorsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DynamicCorsMiddleware> _logger;
        private readonly CorsMiddleware _corsMiddleware;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicCorsMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        /// <param name="corsService">Instance of the <see cref="ICorsService"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="policyName">The cors policy name.</param>
        public DynamicCorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ILoggerFactory loggerFactory,
            string policyName)
        {
            _corsMiddleware = new CorsMiddleware(next, corsService, loggerFactory, policyName);
            _next = next;
            _logger = loggerFactory.CreateLogger<DynamicCorsMiddleware>();
        }

        /// <summary>
        /// Invoke request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="corsPolicyProvider">Instance of the <see cref="ICorsPolicyProvider"/> interface.</param>
        /// <returns>Task.</returns>
        ///
        public async Task Invoke(HttpContext context, ICorsPolicyProvider corsPolicyProvider)
        {
            // Only execute if is preflight request.
            if (string.Equals(context.Request.Method, CorsConstants.PreflightHttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                // Invoke original cors middleware.
                await _corsMiddleware.Invoke(context, corsPolicyProvider).ConfigureAwait(false);
                if (context.Response.Headers.TryGetValue(HeaderNames.AccessControlAllowOrigin, out var headerValue)
                    && string.Equals(headerValue, "*", StringComparison.Ordinal))
                {
                    context.Response.Headers[HeaderNames.AccessControlAllowOrigin] = context.Request.Host.Value;
                    _logger.LogDebug("Overwriting CORS response header: {HeaderName}: {HeaderValue}", HeaderNames.AccessControlAllowOrigin, context.Request.Host.Value);

                    if (!context.Response.Headers.ContainsKey(HeaderNames.AccessControlAllowCredentials))
                    {
                        context.Response.Headers[HeaderNames.AccessControlAllowCredentials] = "true";
                    }
                }
            }

            // Call the next delegate/middleware in the pipeline
            await this._next(context).ConfigureAwait(false);
        }
    }
}
