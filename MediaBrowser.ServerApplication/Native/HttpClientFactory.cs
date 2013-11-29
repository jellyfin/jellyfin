using System;
using System.Net;
using System.Net.Cache;
using System.Net.Http;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class HttpClientFactory
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <param name="enableHttpCompression">if set to <c>true</c> [enable HTTP compression].</param>
        /// <returns>HttpClient.</returns>
        public static HttpClient GetHttpClient(bool enableHttpCompression)
        {
            var client = new HttpClient(new WebRequestHandler
            {
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate),
                AutomaticDecompression = enableHttpCompression ? DecompressionMethods.Deflate : DecompressionMethods.None
                 
            })
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");

            return client;
        }
    }
}
