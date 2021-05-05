using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// URL decodes the querystring before binding.
    /// </summary>
    public class QueryStringDecodingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<QueryStringDecodingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringDecodingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public QueryStringDecodingMiddleware(
            RequestDelegate next,
            ILogger<QueryStringDecodingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Features.Set<IQueryFeature>(new UrlDecodeQueryFeature(httpContext.Features.Get<IQueryFeature>()));

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
