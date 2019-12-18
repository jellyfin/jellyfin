using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class ResponseFilter.
    /// </summary>
    public class ResponseFilter
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseFilter"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ResponseFilter(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Filters the response.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="res">The res.</param>
        /// <param name="dto">The dto.</param>
        public void FilterResponse(IRequest req, HttpResponse res, object dto)
        {
            // Try to prevent compatibility view
            res.Headers.Add("Access-Control-Allow-Headers", "Accept, Accept-Language, Authorization, Cache-Control, Content-Disposition, Content-Encoding, Content-Language, Content-Length, Content-MD5, Content-Range, Content-Type, Date, Host, If-Match, If-Modified-Since, If-None-Match, If-Unmodified-Since, Origin, OriginToken, Pragma, Range, Slug, Transfer-Encoding, Want-Digest, X-MediaBrowser-Token, X-Emby-Authorization");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Origin", "*");

            if (dto is Exception exception)
            {
                _logger.LogError(exception, "Error processing request for {RawUrl}", req.RawUrl);

                if (!string.IsNullOrEmpty(exception.Message))
                {
                    var error = exception.Message.Replace(Environment.NewLine, " ", StringComparison.Ordinal);
                    error = RemoveControlCharacters(error);

                    res.Headers.Add("X-Application-Error-Code", error);
                }
            }

            if (dto is IHasHeaders hasHeaders)
            {
                if (!hasHeaders.Headers.ContainsKey(HeaderNames.Server))
                {
                    hasHeaders.Headers[HeaderNames.Server] = "Microsoft-NetCore/2.0, UPnP/1.0 DLNADOC/1.50";
                }

                // Content length has to be explicitly set on on HttpListenerResponse or it won't be happy
                if (hasHeaders.Headers.TryGetValue(HeaderNames.ContentLength, out string contentLength)
                    && !string.IsNullOrEmpty(contentLength))
                {
                    var length = long.Parse(contentLength, CultureInfo.InvariantCulture);

                    if (length > 0)
                    {
                        res.ContentLength = length;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the control characters.
        /// </summary>
        /// <param name="inString">The in string.</param>
        /// <returns>System.String.</returns>
        public static string RemoveControlCharacters(string inString)
        {
            if (inString == null)
            {
                return null;
            }

            var newString = new StringBuilder(inString.Length);

            foreach (var ch in inString)
            {
                if (!char.IsControl(ch))
                {
                    newString.Append(ch);
                }
            }

            return newString.ToString();
        }
    }
}
