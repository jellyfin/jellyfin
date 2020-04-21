using System;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.ExceptionDtos;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        public ExceptionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory.CreateLogger<ExceptionMiddleware>() ??
                      throw new ArgumentNullException(nameof(loggerFactory));
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

                var exceptionBody = new ExceptionDto { Message = ex.Message };
                var exceptionJson = JsonSerializer.Serialize(exceptionBody);

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                // TODO switch between PascalCase and camelCase
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(exceptionJson).ConfigureAwait(false);
            }
        }
    }
}
