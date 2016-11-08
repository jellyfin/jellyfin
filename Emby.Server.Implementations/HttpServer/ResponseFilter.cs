using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.Text;
using Emby.Server.Implementations.HttpServer.SocketSharp;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.HttpServer
{
    public class ResponseFilter
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;

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
        public void FilterResponse(IRequest req, IResponse res, object dto)
        {
            // Try to prevent compatibility view
            res.AddHeader("X-UA-Compatible", "IE=Edge");
            res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization");
            res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            res.AddHeader("Access-Control-Allow-Origin", "*");

            var exception = dto as Exception;

            if (exception != null)
            {
                _logger.ErrorException("Error processing request for {0}", exception, req.RawUrl);

                if (!string.IsNullOrEmpty(exception.Message))
                {
                    var error = exception.Message.Replace(Environment.NewLine, " ");
                    error = RemoveControlCharacters(error);

                    res.AddHeader("X-Application-Error-Code", error);
                }
            }

            var vary = "Accept-Encoding";

            var hasHeaders = dto as IHasHeaders;
            var sharpResponse = res as WebSocketSharpResponse;

            if (hasHeaders != null)
            {
                if (!hasHeaders.Headers.ContainsKey("Server"))
                {
                    hasHeaders.Headers["Server"] = "Mono-HTTPAPI/1.1, UPnP/1.0 DLNADOC/1.50";
                    //hasHeaders.Headers["Server"] = "Mono-HTTPAPI/1.1";
                }

                // Content length has to be explicitly set on on HttpListenerResponse or it won't be happy
                string contentLength;

                if (hasHeaders.Headers.TryGetValue("Content-Length", out contentLength) && !string.IsNullOrEmpty(contentLength))
                {
                    var length = long.Parse(contentLength, UsCulture);

                    if (length > 0)
                    {
                        res.SetContentLength(length);
                        
                        //var listenerResponse = res.OriginalResponse as HttpListenerResponse;

                        //if (listenerResponse != null)
                        //{
                        //    // Disable chunked encoding. Technically this is only needed when using Content-Range, but
                        //    // anytime we know the content length there's no need for it
                        //    listenerResponse.SendChunked = false;
                        //    return;
                        //}

                        if (sharpResponse != null)
                        {
                            sharpResponse.SendChunked = false;
                        }
                    }
                }

                string hasHeadersVary;
                if (hasHeaders.Headers.TryGetValue("Vary", out hasHeadersVary))
                {
                    vary = hasHeadersVary;
                }

                hasHeaders.Headers["Vary"] = vary;
            }

            //res.KeepAlive = false;

            // Per Google PageSpeed
            // This instructs the proxies to cache two versions of the resource: one compressed, and one uncompressed. 
            // The correct version of the resource is delivered based on the client request header. 
            // This is a good choice for applications that are singly homed and depend on public proxies for user locality.                        
            res.AddHeader("Vary", vary);
        }

        /// <summary>
        /// Removes the control characters.
        /// </summary>
        /// <param name="inString">The in string.</param>
        /// <returns>System.String.</returns>
        public static string RemoveControlCharacters(string inString)
        {
            if (inString == null) return null;

            var newString = new StringBuilder();

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
