using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Middleware for handling OPTIONS requests.
    /// </summary>
    public class CorsOptionsResponseMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsOptionsResponseMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        public CorsOptionsResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            if (string.Equals(httpContext.Request.Method, HttpMethods.Options, StringComparison.OrdinalIgnoreCase))
            {
                httpContext.Response.StatusCode = 200;
                foreach (var (key, value) in GetDefaultCorsHeaders(httpContext))
                {
                    httpContext.Response.Headers.Add(key, value);
                }

                httpContext.Response.ContentType = MediaTypeNames.Text.Plain;
                await httpContext.Response.WriteAsync(string.Empty, httpContext.RequestAborted).ConfigureAwait(false);
                return;
            }

            await _next(httpContext).ConfigureAwait(false);
        }

        private static IDictionary<string, string> GetDefaultCorsHeaders(HttpContext httpContext)
        {
            var origin = httpContext.Request.Headers["Origin"];
            if (origin == StringValues.Empty)
            {
                origin = httpContext.Request.Headers["Host"];
                if (origin == StringValues.Empty)
                {
                    origin = "*";
                }
            }

            var headers = new Dictionary<string, string>();
            headers.Add("Access-Control-Allow-Origin", origin);
            headers.Add("Access-Control-Allow-Credentials", "true");
            headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization, Cookie");
            return headers;
        }
    }
}
