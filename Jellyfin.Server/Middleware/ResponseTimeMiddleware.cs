using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Response time middleware.
    /// </summary>
    public class ResponseTimeMiddleware
    {
        private const string ResponseHeaderResponseTime = "X-Response-Time-ms";

        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseTimeMiddleware> _logger;

        private readonly bool _enableWarning;
        private readonly long _warningThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseTimeMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ExceptionMiddleware}"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ResponseTimeMiddleware(
            RequestDelegate next,
            ILogger<ResponseTimeMiddleware> logger,
            IServerConfigurationManager serverConfigurationManager)
        {
            _next = next;
            _logger = logger;

            _enableWarning = serverConfigurationManager.Configuration.EnableSlowResponseWarning;
            _warningThreshold = serverConfigurationManager.Configuration.SlowResponseThresholdMs;
        }

        /// <summary>
        /// Invoke request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <returns>Task.</returns>
        public async Task Invoke(HttpContext context)
        {
            var watch = new Stopwatch();
            watch.Start();

            context.Response.OnStarting(() =>
            {
                watch.Stop();
                LogWarning(context, watch);
                var responseTimeForCompleteRequest = watch.ElapsedMilliseconds;
                context.Response.Headers[ResponseHeaderResponseTime] = responseTimeForCompleteRequest.ToString(CultureInfo.InvariantCulture);
                return Task.CompletedTask;
            });

            // Call the next delegate/middleware in the pipeline
            await this._next(context).ConfigureAwait(false);
        }

        private void LogWarning(HttpContext context, Stopwatch watch)
        {
            if (_enableWarning && watch.ElapsedMilliseconds > _warningThreshold)
            {
                _logger.LogWarning(
                    "Slow HTTP Response from {url} to {remoteIp} in {elapsed:g} with Status Code {statusCode}",
                    context.Request.GetDisplayUrl(),
                    context.GetNormalizedRemoteIp(),
                    watch.Elapsed,
                    context.Response.StatusCode);
            }
        }
    }
}
