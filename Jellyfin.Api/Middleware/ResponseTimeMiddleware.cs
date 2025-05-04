using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Response time middleware.
/// </summary>
public class ResponseTimeMiddleware
{
    private const string ResponseHeaderResponseTime = "X-Response-Time-ms";

    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseTimeMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next request delegate.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ExceptionMiddleware}"/> interface.</param>
    public ResponseTimeMiddleware(
        RequestDelegate next,
        ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke request.
    /// </summary>
    /// <param name="context">Request context.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <returns>Task.</returns>
    public async Task Invoke(HttpContext context, IServerConfigurationManager serverConfigurationManager)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        var enableWarning = serverConfigurationManager.Configuration.EnableSlowResponseWarning;
        var warningThreshold = serverConfigurationManager.Configuration.SlowResponseThresholdMs;
        context.Response.OnStarting(() =>
        {
            var responseTime = Stopwatch.GetElapsedTime(startTimestamp);
            var responseTimeMs = responseTime.TotalMilliseconds;
            if (enableWarning && responseTimeMs > warningThreshold && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Slow HTTP Response from {Url} to {RemoteIP} in {Elapsed:g} with Status Code {StatusCode}",
                    context.Request.GetDisplayUrl(),
                    context.GetNormalizedRemoteIP(),
                    responseTime,
                    context.Response.StatusCode);
            }

            context.Response.Headers[ResponseHeaderResponseTime] = responseTimeMs.ToString(CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        // Call the next delegate/middleware in the pipeline
        await this._next(context).ConfigureAwait(false);
    }
}
