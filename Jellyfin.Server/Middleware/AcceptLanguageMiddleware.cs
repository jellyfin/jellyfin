using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware;

/// <summary>
/// Middleware that resolves the <c>Accept-Language</c> request header
/// to an ordered list of Jellyfin-supported cultures, sets the fallback chain
/// on <see cref="LocalizationManager.RequestCultureFallback"/>, and writes
/// the <c>Content-Language</c> response header.
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
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <returns>Task.</returns>
    public async Task Invoke(HttpContext context, IServerConfigurationManager configurationManager)
    {
        var chain = ResolveLanguages(context.Request, configurationManager);
        if (chain is not null)
        {
            LocalizationManager.RequestCultureFallback = chain;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(chain[0]);
        }

        context.Response.OnStarting(
            static state =>
            {
                var (ctx, languages) = ((HttpContext, IReadOnlyList<string>?))state;
                if (languages is not null)
                {
                    ctx.Response.Headers.ContentLanguage = string.Join(", ", languages);
                }
                else
                {
                    var culture = CultureInfo.CurrentUICulture.Name;
                    if (!string.IsNullOrEmpty(culture))
                    {
                        ctx.Response.Headers.ContentLanguage = culture;
                    }
                }

                return Task.CompletedTask;
            },
            (context, chain));

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            LocalizationManager.RequestCultureFallback = null;
        }
    }

    private static IReadOnlyList<string>? ResolveLanguages(HttpRequest request, IServerConfigurationManager configurationManager)
    {
        var acceptLanguageHeader = request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguageHeader is null || acceptLanguageHeader.Count == 0)
        {
            return null;
        }

        var languages = acceptLanguageHeader
            .OrderByDescending(h => h.Quality ?? 1)
            .Select(h => h.Value.ToString());

        var chain = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lang in languages)
        {
            TryAddCulture(lang, chain, seen);
        }

        // Append server default culture if not already present
        var serverCulture = configurationManager.Configuration.UICulture;
        if (!string.IsNullOrEmpty(serverCulture))
        {
            TryAddCulture(serverCulture, chain, seen);
        }

        // Ensure en-US is always the final fallback
        TryAddCulture("en-US", chain, seen);

        return chain;
    }

    private static void TryAddCulture(string lang, List<string> chain, HashSet<string> seen)
    {
        // Direct match
        if (LocalizationManager.HasTranslation(lang) && seen.Add(lang))
        {
            chain.Add(lang);
            return;
        }

        // BCP-47 to Jellyfin underscore mapping (e.g. es-419 -> es_419)
        if (LocalizationManager.Bcp47ToJellyfinMap.TryGetValue(lang, out var mapped) && seen.Add(mapped))
        {
            chain.Add(mapped);
            return;
        }

        // Parent culture fallback (e.g. de-DE -> de)
        var dashIndex = lang.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            var parent = lang[..dashIndex];
            if (LocalizationManager.HasTranslation(parent) && seen.Add(parent))
            {
                chain.Add(parent);
            }
        }
    }
}
