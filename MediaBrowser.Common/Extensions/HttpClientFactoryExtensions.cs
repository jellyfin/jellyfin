using System;
using System.Net.Http;
using MediaBrowser.Common.Plugins;

namespace MediaBrowser.Common.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceProvider"/>.
/// </summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Get a plugin-configured HttpClient.
    /// This requires calling <c>AddHttpClient{T}</c> during <c>RegisterServices</c>.
    /// </summary>
    /// <param name="httpClientFactory">The http client factory.</param>
    /// <typeparam name="T">The type of plugin.</typeparam>
    /// <returns>The HttpClient.</returns>
    public static HttpClient GetPluginHttpClient<T>(this IHttpClientFactory httpClientFactory)
        where T : BasePlugin
        => httpClientFactory.CreateClient(typeof(T).Name);
}
