using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Text;

namespace MediaBrowser.Server.Implementations.HttpServer
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
            var log = new StringBuilder();

            //var headers = string.Join(",", request.Headers.AllKeys.Where(i => !string.Equals(i, "cookie", StringComparison.OrdinalIgnoreCase) && !string.Equals(i, "Referer", StringComparison.OrdinalIgnoreCase)).Select(k => k + "=" + request.Headers[k]));

            //log.AppendLine("Ip: " + request.RemoteEndPoint + ". Headers: " + headers);

            var type = request.IsWebSocketRequest ? "Web Socket" : "HTTP " + request.HttpMethod;

            logger.LogMultiline(type + " " + request.Url, LogSeverity.Debug, log);
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="url">The URL.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="duration">The duration.</param>
        public static void LogResponse(ILogger logger, int statusCode, string url, string endPoint, TimeSpan duration)
        {
            var log = new StringBuilder();

            log.AppendLine(string.Format("Url: {0}", url));

            //log.AppendLine("Headers: " + string.Join(",", response.Headers.AllKeys.Select(k => k + "=" + response.Headers[k])));

            var responseTime = string.Format(". Response time: {0} ms.", duration.TotalMilliseconds);

            var msg = "HTTP Response " + statusCode + " to " + endPoint + responseTime;

            logger.LogMultiline(msg, LogSeverity.Debug, log);
        }
    }
}
