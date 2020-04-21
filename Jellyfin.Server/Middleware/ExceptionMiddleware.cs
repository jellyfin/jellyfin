using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Exception Middleware.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IServerConfigurationManager _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ExceptionMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IServerConfigurationManager serverConfigurationManager)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ExceptionMiddleware>();
            _configuration = serverConfigurationManager;
        }

        /// <summary>
        /// Invoke request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <returns>Task.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the exception middleware will not be executed.");
                    throw;
                }

                ex = GetActualException(ex);
                _logger.LogError(ex, "Error processing request: {0}", ex.Message);
                context.Response.StatusCode = GetStatusCode(ex);
                context.Response.ContentType = "text/plain";

                var errorContent = NormalizeExceptionMessage(ex.Message);
                await context.Response.WriteAsync(errorContent).ConfigureAwait(false);
            }
        }

        private static Exception GetActualException(Exception ex)
        {
            if (ex is AggregateException agg)
            {
                var inner = agg.InnerException;
                if (inner != null)
                {
                    return GetActualException(inner);
                }

                var inners = agg.InnerExceptions;
                if (inners.Count > 0)
                {
                    return GetActualException(inners[0]);
                }
            }

            return ex;
        }

        private static int GetStatusCode(Exception ex)
        {
            switch (ex)
            {
                case ArgumentException _: return StatusCodes.Status400BadRequest;
                case SecurityException _: return StatusCodes.Status401Unauthorized;
                case DirectoryNotFoundException _:
                case FileNotFoundException _:
                case ResourceNotFoundException _: return StatusCodes.Status404NotFound;
                case MethodNotAllowedException _: return StatusCodes.Status405MethodNotAllowed;
                default: return StatusCodes.Status500InternalServerError;
            }
        }

        private string NormalizeExceptionMessage(string msg)
        {
            if (msg == null)
            {
                return string.Empty;
            }

            // Strip any information we don't want to reveal
            msg = msg.Replace(
                _configuration.ApplicationPaths.ProgramSystemPath,
                string.Empty,
                StringComparison.OrdinalIgnoreCase);
            msg = msg.Replace(
                _configuration.ApplicationPaths.ProgramDataPath,
                string.Empty,
                StringComparison.OrdinalIgnoreCase);

            return msg;
        }
    }
}
