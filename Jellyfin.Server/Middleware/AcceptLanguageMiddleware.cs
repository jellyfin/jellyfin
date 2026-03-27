using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware;

/// <summary>
/// Middleware that resolves the <c>Accept-Language</c> request header
/// to a Jellyfin-supported culture and sets <see cref="CultureInfo.CurrentUICulture"/>
/// for the duration of the request. Also sets the <c>Content-Language</c> response header.
/// </summary>
public class AcceptLanguageMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptLanguageMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next request delegate.</param>
    public AcceptLanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invoke request.
    /// </summary>
    /// <param name="context">Request context.</param>
    /// <returns>Task.</returns>
    public async Task Invoke(HttpContext context)
    {
        var resolved = ResolveLanguage(context.Request);
        if (resolved is not null)
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(resolved);
        }

        context.Response.OnStarting(
            static state =>
            {
                var ctx = (HttpContext)state;
                var culture = CultureInfo.CurrentUICulture.Name;
                if (!string.IsNullOrEmpty(culture))
                {
                    ctx.Response.Headers.ContentLanguage = culture;
                }

                return Task.CompletedTask;
            },
            context);

        await _next(context).ConfigureAwait(false);
    }

    private static string? ResolveLanguage(HttpRequest request)
    {
        var acceptLanguageHeader = request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguageHeader is null || acceptLanguageHeader.Count == 0)
        {
            return null;
        }

        var languages = acceptLanguageHeader
            .OrderByDescending(h => h.Quality ?? 1)
            .Select(h => h.Value.ToString());

        foreach (var lang in languages)
        {
            if (LocalizationManager.HasTranslation(lang))
            {
                return lang;
            }

            if (LocalizationManager.Bcp47ToJellyfinMap.TryGetValue(lang, out var mapped))
            {
                return mapped;
            }

            // Try parent culture match (e.g. de-DE -> de)
            var dashIndex = lang.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                var parent = lang[..dashIndex];
                if (LocalizationManager.HasTranslation(parent))
                {
                    return parent;
                }
            }
        }

        return null;
    }
}
