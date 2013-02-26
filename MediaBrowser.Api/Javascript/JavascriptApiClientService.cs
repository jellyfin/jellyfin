using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Networking.HttpServer;
using ServiceStack.ServiceHost;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Javascript
{
    /// <summary>
    /// Class GetJavascriptApiClient
    /// </summary>
    [Route("/JsApiClient.js", "GET")]
    [ServiceStack.ServiceHost.Api(("Gets an api wrapper in Javascript"))]
    public class GetJavascriptApiClient
    {
        /// <summary>
        /// Version identifier for caching
        /// </summary>
        /// <value>The v.</value>
        public string V { get; set; }
    }

    /// <summary>
    /// Class JavascriptApiClientService
    /// </summary>
    public class JavascriptApiClientService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetJavascriptApiClient request)
        {
            TimeSpan? cacheDuration = null;

            // If there's a version number in the query string we can cache this unconditionally
            if (!string.IsNullOrEmpty(request.V))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var assembly = GetType().Assembly.GetName();

            return ToStaticResult(assembly.Version.ToString().GetMD5(), null, cacheDuration, MimeTypes.GetMimeType("script.js"), GetStream);
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>Stream.</returns>
        private Task<Stream> GetStream()
        {
            return Task.FromResult(GetType().Assembly.GetManifestResourceStream("MediaBrowser.Api.Javascript.ApiClient.js"));
        }
    }
}
