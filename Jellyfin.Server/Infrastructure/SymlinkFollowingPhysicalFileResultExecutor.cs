// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Infrastructure
{
    /// <inheritdoc />
    public class SymlinkFollowingPhysicalFileResultExecutor : PhysicalFileResultExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymlinkFollowingPhysicalFileResultExecutor"/> class.
        /// </summary>
        /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
        public SymlinkFollowingPhysicalFileResultExecutor(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        /// <inheritdoc />
        protected override FileMetadata GetFileInfo(string path)
        {
            var fileInfo = new FileInfo(path);
            var length = fileInfo.Length;
            // This may or may not be fixed in .NET 6, but looks like it will not https://github.com/dotnet/aspnetcore/issues/34371
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                using var fileHandle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                length = RandomAccess.GetLength(fileHandle);
            }

            return new FileMetadata
            {
                Exists = fileInfo.Exists,
                Length = length,
                LastModified = fileInfo.LastWriteTimeUtc
            };
        }

        /// <inheritdoc />
        protected override async Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue? range, long rangeLength)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(result);

            if (range is not null && rangeLength == 0)
            {
                return;
            }

            // It's a bit of wasted IO to perform this check again, but non-symlinks shouldn't use this code
            if (!IsSymLink(result.FileName))
            {
                await base.WriteFileAsync(context, result, range, rangeLength).ConfigureAwait(false);
                return;
            }

            var response = context.HttpContext.Response;

            if (range is not null)
            {
                await SendFileAsync(
                    result.FileName,
                    response,
                    offset: range.From ?? 0L,
                    count: rangeLength).ConfigureAwait(false);
                return;
            }

            await SendFileAsync(
                result.FileName,
                response,
                offset: 0,
                count: null).ConfigureAwait(false);
        }

        private async Task SendFileAsync(string filePath, HttpResponse response, long offset, long? count, CancellationToken cancellationToken = default)
        {
            var fileInfo = GetFileInfo(filePath);
            if (offset < 0 || offset > fileInfo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }

            if (count.HasValue
                && (count.Value < 0 || count.Value > fileInfo.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }

            // Copied from SendFileFallback.SendFileAsync
            const int BufferSize = 1024 * 16;

            var useRequestAborted = !cancellationToken.CanBeCanceled;
            var localCancel = useRequestAborted ? response.HttpContext.RequestAborted : cancellationToken;

            var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: BufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using (fileStream.ConfigureAwait(false))
            {
                try
                {
                    localCancel.ThrowIfCancellationRequested();
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    await StreamCopyOperation
                        .CopyToAsync(fileStream, response.Body, count, BufferSize, localCancel)
                        .ConfigureAwait(true);
                }
                catch (OperationCanceledException) when (useRequestAborted)
                {
                }
            }
        }

        private static bool IsSymLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }
}
