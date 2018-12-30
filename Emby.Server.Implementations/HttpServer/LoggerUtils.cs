using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.HttpServer
{
    public static class LoggerUtils
    {
        public static void LogRequest(ILogger logger, string url, string method, string userAgent, QueryParamCollection headers)
        {
            if (headers == null)
            {
                logger.LogInformation("{0} {1}. UserAgent: {2}", "HTTP " + method, url, userAgent ?? string.Empty);
            }
            else
            {
                var headerText = string.Empty;
                var index = 0;

                foreach (var i in headers)
                {
                    if (index > 0)
                    {
                        headerText += ", ";
                    }

                    headerText += i.Name + "=" + i.Value;

                    index++;
                }

                logger.LogInformation("HTTP {0} {1}. {2}", method, url, headerText);
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

            //var headerText = headers == null ? string.Empty : "Headers: " + string.Join(", ", headers.Where(i => i.Name.IndexOf("Access-", StringComparison.OrdinalIgnoreCase) == -1).Select(i => i.Name + "=" + i.Value).ToArray());
            var headerText = string.Empty;
            logger.LogInformation("HTTP Response {0} to {1}. Time: {2}{3}. {4} {5}", statusCode, endPoint, Convert.ToInt32(durationMs).ToString(CultureInfo.InvariantCulture), logSuffix, url, headerText);
        }
    }
}
