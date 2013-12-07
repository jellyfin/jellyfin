using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public static class LoggerUtils
    {
        public static void LogRequest(ILogger logger, HttpListenerContext ctx, int workerIndex)
        {
            var log = new StringBuilder();

            log.AppendLine("Url: " + ctx.Request.Url);
            log.AppendLine("Headers: " + string.Join(",", ctx.Request.Headers.AllKeys.Select(k => k + "=" + ctx.Request.Headers[k])));

            var type = ctx.Request.IsWebSocketRequest ? "Web Socket" : "HTTP " + ctx.Request.HttpMethod;

            logger.LogMultiline(type + " request received on worker " + workerIndex + " from " + ctx.Request.RemoteEndPoint, LogSeverity.Debug, log);
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="url">The URL.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="duration">The duration.</param>
        public static void LogResponse(ILogger logger, HttpListenerContext ctx, string url, IPEndPoint endPoint, TimeSpan duration)
        {
            var statusCode = ctx.Response.StatusCode;

            var log = new StringBuilder();

            log.AppendLine(string.Format("Url: {0}", url));

            log.AppendLine("Headers: " + string.Join(",", ctx.Response.Headers.AllKeys.Select(k => k + "=" + ctx.Response.Headers[k])));

            var responseTime = string.Format(". Response time: {0} ms", duration.TotalMilliseconds);

            var msg = "Response code " + statusCode + " sent to " + endPoint + responseTime;

            logger.LogMultiline(msg, LogSeverity.Debug, log);
        }
    }
}
