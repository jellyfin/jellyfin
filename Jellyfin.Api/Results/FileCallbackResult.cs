using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Results
{
    /// <summary>
    /// FileCallbackResult to allow for streaming files with custom actions (i.e. zip compression).
    /// </summary>
    public class FileCallbackResult : FileResult
    {
        private readonly Func<Stream, ActionContext, Task> _callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCallbackResult"/> class.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        /// <param name="callback">The callback.</param>
        public FileCallbackResult(MediaTypeHeaderValue? contentType, Func<Stream, ActionContext, Task>? callback)
            : base(contentType?.ToString() ?? string.Empty)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _callback = callback;
        }

        /// <inheritdoc/>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null, false);
                return result._callback(context.HttpContext.Response.BodyWriter.AsStream(), context);
            }
        }
    }
}
