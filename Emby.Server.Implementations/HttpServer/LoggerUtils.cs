using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.Services;
using SocketHttpListener.Net;

namespace Emby.Server.Implementations.HttpServer
{
    public static class LoggerUtils
    {
        /// <summary>
        /// Logs the request.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="request">The request.</param>
        public static void LogRequest(ILogger logger, HttpListenerRequest request)
        {
            var url = request.Url.ToString();

            logger.Info("{0} {1}. UserAgent: {2}", request.IsWebSocketRequest ? "WS" : "HTTP " + request.HttpMethod, url, request.UserAgent ?? string.Empty);
        }

        public static void LogRequest(ILogger logger, string url, string method, string userAgent, QueryParamCollection headers)
        {
            if (headers == null)
            {
                logger.Info("{0} {1}. UserAgent: {2}", "HTTP " + method, url, userAgent ?? string.Empty);
            }
            else
            {
                var headerText = string.Join(", ", headers.Select(i => i.Name + "=" + i.Value).ToArray());

                logger.Info("HTTP {0} {1}. {2}", method, url, headerText);
            }
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="url">The URL.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="duration">The duration.</param>
        public static void LogResponse(ILogger logger, int statusCode, string url, string endPoint, TimeSpan duration, QueryParamCollection headers)
        {
            var durationMs = duration.TotalMilliseconds;
            var logSuffix = durationMs >= 1000 && durationMs < 60000 ? "ms (slow)" : "ms";

            var headerText = headers == null ? string.Empty : "Headers: " + string.Join(", ", headers.Where(i => i.Name.IndexOf("Access-", StringComparison.OrdinalIgnoreCase) == -1).Select(i => i.Name + "=" + i.Value).ToArray());
            logger.Info("HTTP Response {0} to {1}. Time: {2}{3}. {4} {5}", statusCode, endPoint, Convert.ToInt32(durationMs).ToString(CultureInfo.InvariantCulture), logSuffix, url, headerText);
        }
    }
}
