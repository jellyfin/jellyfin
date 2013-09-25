using System.Net;
using System.Net.Cache;
using System.Net.Http;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class HttpMessageHandlerFactory
    /// </summary>
    public static class HttpMessageHandlerFactory
    {
        /// <summary>
        /// Gets the HTTP message handler.
        /// </summary>
        /// <param name="enableHttpCompression">if set to <c>true</c> [enable HTTP compression].</param>
        /// <returns>HttpMessageHandler.</returns>
        public static HttpMessageHandler GetHttpMessageHandler(bool enableHttpCompression)
        {
			return new HttpClientHandler
            {
                AutomaticDecompression = enableHttpCompression ? DecompressionMethods.Deflate : DecompressionMethods.None
            };
        }
    }
}
