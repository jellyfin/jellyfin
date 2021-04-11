using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Mono.Unix;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// <see cref="PhysicalFileResultExecutor"/> replacement using <see cref="Mono.Unix.UnixFileInfo"/>.
    /// </summary>
    /// This is required until ASP properly handles symbolic links in <see cref="PhysicalFileResultExecutor"/>.
    /// This most likely depends on proper support in .NET <see cref="System.IO.FileInfo"/>. (Currently reports bugus Length)
    /// See https://github.com/dotnet/runtime/projects/41
    public class UnixPhysicalFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<PhysicalFileResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnixPhysicalFileResultExecutor"/> class.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
        public UnixPhysicalFileResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<UnixPhysicalFileResultExecutor>(loggerFactory))
        {
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, PhysicalFileResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var fileInfo = new UnixFileInfo(result.FileName);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Invalid path: " + result.FileName, result.FileName);
            }

            Logger.LogInformation("Sending file {FileName}", fileInfo.Name);

            var lastModified = result.LastModified ?? fileInfo.LastWriteTimeUtc;
            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                fileInfo.Length,
                result.EnableRangeProcessing,
                lastModified,
                result.EntityTag);

            if (serveBody)
            {
                return WriteFileAsyncInternal(context, fileInfo, range, rangeLength);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Write file represented by fileInfo to response body.
        /// </summary>
        /// <param name="context">The action context.</param>
        /// <param name="fileInfo">Mono.Unix.UnixFileInfo representing the file to be sent.</param>
        /// <param name="range">Possibly range header offset.</param>
        /// <param name="rangeLength">Length of byte range.</param>
        private async Task WriteFileAsyncInternal(ActionContext context, UnixFileInfo fileInfo, RangeItemHeaderValue? range, long rangeLength)
        {
            if (range != null && rangeLength == 0)
            {
                return;
            }

            var response = context.HttpContext.Response;
            long offset = range?.From ?? 0L;
            long count = (range == null) ? fileInfo.Length : rangeLength;

            if (offset != 0)
            {
                Logger.LogDebug("Writing out response body range {Offset}+{Count}...", offset, count);
            }

            if (offset < 0 || offset > fileInfo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range, string.Empty);
            }

            if (count < 0 || count > fileInfo.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeLength), rangeLength, string.Empty);
            }

            await response.StartAsync().ConfigureAwait(true);

            const int bufferSize = 1024 * 16;

            var fileStream = new FileStream(
                fileInfo.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: bufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            fileStream.Seek(offset, SeekOrigin.Begin);
            await StreamCopyOperation
                .CopyToAsync(fileStream, response.Body, count, bufferSize, CancellationToken.None)
                .ConfigureAwait(true);
        }
    }
}
