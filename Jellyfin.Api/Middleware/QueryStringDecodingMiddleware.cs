using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// URL decodes the querystring before binding.
/// </summary>
public class QueryStringDecodingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryStringDecodingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    public QueryStringDecodingMiddleware(RequestDelegate next)
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
        // Some clients (e.g. certain DLNA set-top boxes) percent-encode their own query
        // separator as %3F instead of sending a literal '?'. ASP.NET Core decodes that
        // into a literal '?' embedded inside Request.Path, leaving QueryString empty and
        // every [FromQuery] parameter silently defaulted. Left uncorrected, this corrupts
        // downstream logic that assumes Path and QueryString are properly split (e.g. the
        // transcode output file path). Split it out before routing/binding runs.
        var path = httpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(path))
        {
            var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                var embeddedQuery = path[(queryIndex + 1)..];
                httpContext.Request.Path = new PathString(path[..queryIndex]);
                httpContext.Request.QueryString = new QueryString("?" + embeddedQuery);
            }
        }

        var feature = httpContext.Features.Get<IQueryFeature>();
        if (feature is not null)
        {
            httpContext.Features.Set<IQueryFeature>(new UrlDecodeQueryFeature(feature));
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
