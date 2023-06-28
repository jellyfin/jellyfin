using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Provides methods to throttle the download of a physical file.
/// </summary>
public class ThrottledPhysicalFileResultExecutor : PhysicalFileResultExecutor, IActionResultExecutor<ThrottledPhysicalFileActionResult>
{
    private readonly IBandwidthLimiterProviderService _iBandwidthLimiterProviderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledPhysicalFileResultExecutor"/> class.
    /// </summary>
    /// <param name="loggerFactory">The Loggerfactory.</param>
    /// <param name="iBandwidthLimiterProviderService">Service to provide access to the set limits for download.</param>
    public ThrottledPhysicalFileResultExecutor(
        ILoggerFactory loggerFactory,
        IBandwidthLimiterProviderService iBandwidthLimiterProviderService)
        : base(loggerFactory)
    {
        _iBandwidthLimiterProviderService = iBandwidthLimiterProviderService;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(ActionContext context, ThrottledPhysicalFileActionResult result)
    {
        return ExecuteAsync(context, result as PhysicalFileResult);
    }

    /// <inheritdoc />
    protected override Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue? range, long rangeLength)
    {
        return WriteFileAsyncInternal(context.HttpContext, result, range, rangeLength, Logger);
    }

    internal Task WriteFileAsyncInternal(
        HttpContext httpContext,
        PhysicalFileResult result,
        RangeItemHeaderValue? range,
        long rangeLength,
        ILogger logger)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (range != null && rangeLength == 0)
        {
            return Task.CompletedTask;
        }

        var response = httpContext.Response;
        if (!Path.IsPathRooted(result.FileName))
        {
            throw new NotSupportedException("Path is not rooted");
        }

        var userId = httpContext.User.GetUserId();

        if (range != null)
        {
            return SendFileAsync(
                response.Body,
                result.FileName,
                offset: range.From ?? 0L,
                count: rangeLength,
                userId,
                CancellationToken.None);
        }

        return SendFileAsync(
            response.Body,
            result.FileName,
            offset: 0,
            count: null,
            userId,
            CancellationToken.None);
    }

    private async Task SendFileAsync(Stream destination, string filePath, long offset, long? count, Guid requestingUser, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (offset < 0 || offset > fileInfo.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
        }

        if (count.HasValue &&
            (count.Value < 0 || count.Value > fileInfo.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
        }

        cancellationToken.ThrowIfCancellationRequested();

        const int bufferSize = 1024 * 16;

        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: bufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using (fileStream)
        {
            fileStream.Seek(offset, SeekOrigin.Begin);
            await CopyToAsync(fileStream, destination, count, bufferSize, requestingUser, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CopyToAsync(Stream source, Stream destination, long? count, int bufferSize, Guid requestingUser, CancellationToken cancel)
    {
        const int OneSecInMs = 1000;
        var bytesRemaining = count;

        var limiter = _iBandwidthLimiterProviderService.GetLimit(requestingUser);

        void OnIBandwidthLimiterProviderServiceOnBandwidthLimitUpdated(object? sender, BandwidthLimitOptionEventArgs args)
        {
            if (args.BandwidthLimitOption.User.Equals(requestingUser))
            {
                limiter = args.BandwidthLimitOption;
            }
        }

        _iBandwidthLimiterProviderService.BandwidthLimitUpdated += OnIBandwidthLimiterProviderServiceOnBandwidthLimitUpdated;

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var frameWatch = new Stopwatch();
            var bytesWritten = 0;
            while (true)
            {
                // The natural end of the range.
                if (bytesRemaining is <= 0)
                {
                    return;
                }

                cancel.ThrowIfCancellationRequested();

                var readLength = buffer.Length;
                if (bytesRemaining.HasValue)
                {
                    readLength = (int)Math.Min(bytesRemaining.GetValueOrDefault(), (long)readLength);
                }

                var read = await source.ReadAsync(buffer.AsMemory(0, readLength), cancel)
                    .ConfigureAwait(false);

                if (bytesRemaining.HasValue)
                {
                    bytesRemaining -= read;
                }

                bytesWritten += read;
                if (bytesWritten > limiter.BandwidthPerSec && limiter.BandwidthPerSec > 0)
                {
                    await Task.Delay((int)(OneSecInMs - frameWatch.ElapsedMilliseconds) + 1, cancel)
                        .ConfigureAwait(false);
                    bytesWritten = 0;
                }

                if (frameWatch.ElapsedMilliseconds > OneSecInMs)
                {
                    frameWatch.Restart();
                }

                // End of the source stream.
                if (read == 0)
                {
                    return;
                }

                cancel.ThrowIfCancellationRequested();

                await destination.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _iBandwidthLimiterProviderService.BandwidthLimitUpdated -= OnIBandwidthLimiterProviderServiceOnBandwidthLimitUpdated;
        }
    }
}
