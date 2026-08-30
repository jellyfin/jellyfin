using System;
using System.IO;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Exception Middleware.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IServerConfigurationManager _configuration;
    private readonly IWebHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next request delegate.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ExceptionMiddleware}"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="hostEnvironment">Instance of the <see cref="IWebHostEnvironment"/> interface.</param>
    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IServerConfigurationManager serverConfigurationManager,
        IWebHostEnvironment hostEnvironment)
    {
        _next = next;
        _logger = logger;
        _configuration = serverConfigurationManager;
        _hostEnvironment = hostEnvironment;
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

            bool ignoreStackTrace =
                ex is SocketException
                || ex is IOException
                || ex is OperationCanceledException
                || ex is SecurityException
                || ex is AuthenticationException
                || ex is FileNotFoundException;

            if (ignoreStackTrace)
            {
                _logger.LogError(
                    "Error processing request: {ExceptionMessage}. URL {Method} {Url}.",
                    ex.Message.TrimEnd('.'),
                    context.Request.Method,
                    context.Request.Path);
            }
            else
            {
                _logger.LogError(
                    ex,
                    "Error processing request. URL {Method} {Url}.",
                    context.Request.Method,
                    context.Request.Path);
            }

            context.Response.StatusCode = GetStatusCode(ex);
            context.Response.ContentType = MediaTypeNames.Text.Plain;

            // Tell the client why it was refused. Without this a parental-control
            // rejection is an indistinguishable 403 and clients render it as an
            // empty library rather than an actionable message.
            if (IsParentalControlRejection(ex))
            {
                context.Response.Headers[ApplicationErrorCodes.HeaderName] = ApplicationErrorCodes.ParentalControl;
            }

            // Don't send exception unless the server is in a Development environment
            var errorContent = _hostEnvironment.IsDevelopment()
                    ? NormalizeExceptionMessage(ex.Message)
                    : "Error processing request.";
            await context.Response.WriteAsync(errorContent).ConfigureAwait(false);
        }
    }

    private static Exception GetActualException(Exception ex)
    {
        if (ex is AggregateException agg)
        {
            var inner = agg.InnerException;
            if (inner is not null)
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

    /// <summary>
    /// Checks whether an exception, or anything it wraps, is a parental control rejection.
    /// </summary>
    /// <remarks>
    /// The chain has to be walked because <c>UserController</c> catches
    /// <see cref="SecurityException"/> and rethrows a plain one to prefix the remote IP to
    /// the message, which erases the original type.
    /// </remarks>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns><c>true</c> if the exception or any inner exception is a <see cref="ParentalControlException"/>.</returns>
    private static bool IsParentalControlRejection(Exception? ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is ParentalControlException)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetStatusCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            AuthenticationException => StatusCodes.Status401Unauthorized,
            SecurityException => StatusCodes.Status403Forbidden,
            DirectoryNotFoundException => StatusCodes.Status404NotFound,
            FileNotFoundException => StatusCodes.Status404NotFound,
            ResourceNotFoundException => StatusCodes.Status404NotFound,
            MethodNotAllowedException => StatusCodes.Status405MethodNotAllowed,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private string NormalizeExceptionMessage(string msg)
    {
        // Strip any information we don't want to reveal
        return msg.Replace(
                _configuration.ApplicationPaths.ProgramSystemPath,
                string.Empty,
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                _configuration.ApplicationPaths.ProgramDataPath,
                string.Empty,
                StringComparison.OrdinalIgnoreCase);
    }
}
