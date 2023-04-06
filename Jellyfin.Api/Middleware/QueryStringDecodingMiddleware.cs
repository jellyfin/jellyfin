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
        var feature = httpContext.Features.Get<IQueryFeature>();
        if (feature is not null)
        {
            httpContext.Features.Set<IQueryFeature>(new UrlDecodeQueryFeature(feature));
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
