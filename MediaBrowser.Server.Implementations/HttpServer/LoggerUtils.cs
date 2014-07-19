using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Text;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public static class LoggerUtils
    {
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
