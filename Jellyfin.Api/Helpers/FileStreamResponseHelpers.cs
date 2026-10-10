using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// The stream response helpers.
/// </summary>
public static class FileStreamResponseHelpers
{
    private static readonly AsyncKeyedLocker<string> _blurayProbeLocks = new();

    /// <summary>
    /// Returns a static file from a remote source.
    /// </summary>
    /// <param name="state">The current <see cref="StreamState"/>.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> making the remote request.</param>
    /// <param name="httpContext">The current http context.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
    public static async Task<ActionResult> GetStaticRemoteStreamResult(
        StreamState state,
        HttpClient httpClient,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(state.MediaPath));

        // Forward User-Agent if provided
        if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
        {
            // Clear default and add specific one if exists, otherwise HttpClient default might be used
            requestMessage.Headers.UserAgent.Clear();
            requestMessage.Headers.TryAddWithoutValidation(HeaderNames.UserAgent, useragent);
        }

        // Forward Range header if present in the client request
        if (httpContext.Request.Headers.TryGetValue(HeaderNames.Range, out var rangeValue))
        {
            var rangeString = rangeValue.ToString();
            if (!string.IsNullOrEmpty(rangeString))
            {
                requestMessage.Headers.Range = System.Net.Http.Headers.RangeHeaderValue.Parse(rangeString);
            }
        }

        // Send the request to the upstream server
        // Use ResponseHeadersRead to avoid downloading the whole content immediately
        var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        // Check if the upstream server supports range requests and acted upon our Range header
        bool upstreamSupportsRange = response.StatusCode == System.Net.HttpStatusCode.PartialContent;
        string acceptRangesValue = "none";
        if (response.Headers.TryGetValues(HeaderNames.AcceptRanges, out var acceptRangesHeaders))
        {
            // Prefer upstream server's Accept-Ranges header if available
            acceptRangesValue = string.Join(", ", acceptRangesHeaders);
            upstreamSupportsRange |= acceptRangesValue.Contains("bytes", StringComparison.OrdinalIgnoreCase);
        }
        else if (upstreamSupportsRange) // If we got 206 but no Accept-Ranges header, assume bytes
        {
            acceptRangesValue = "bytes";
        }

        // Set Accept-Ranges header for the client based on upstream support
        httpContext.Response.Headers[HeaderNames.AcceptRanges] = acceptRangesValue;

        // Set Content-Range header if upstream provided it (implies partial content)
        if (response.Content.Headers.ContentRange is not null)
        {
            httpContext.Response.Headers[HeaderNames.ContentRange] = response.Content.Headers.ContentRange.ToString();
        }

        // Set Content-Length header. For partial content, this is the length of the partial segment.
        if (response.Content.Headers.ContentLength.HasValue)
        {
            httpContext.Response.ContentLength = response.Content.Headers.ContentLength.Value;
        }

        // Set Content-Type header
        var contentType = response.Content.Headers.ContentType?.ToString() ?? MediaTypeNames.Application.Octet; // Use a more generic default

        // Set the status code for the client response (e.g., 200 OK or 206 Partial Content)
        httpContext.Response.StatusCode = (int)response.StatusCode;

        // Return the stream from the upstream server
        // IMPORTANT: Do not dispose the response stream here, FileStreamResult will handle it.
        return new FileStreamResult(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), contentType);
    }

    /// <summary>
    /// Returns a static file from the server.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <returns>An <see cref="ActionResult"/> the file.</returns>
    public static ActionResult GetStaticFileResult(
        string path,
        string contentType)
    {
        return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
    }

    /// <summary>
    /// Returns the original clip for a single-file Blu-ray title when its duration matches the title.
    /// </summary>
    /// <param name="source">The Blu-ray media source.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="memoryCache">The media probe cache.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A static file result, or null when the title requires playlist processing.</returns>
    public static async Task<ActionResult?> GetStaticBlurayFileResult(
        MediaSourceInfo source,
        IMediaEncoder mediaEncoder,
        IMemoryCache memoryCache,
        CancellationToken cancellationToken)
    {
        var files = mediaEncoder.GetPrimaryPlaylistM2tsFiles(source.Path);
        if (files is not { Count: 1 } || source.RunTimeTicks is not > 0)
        {
            return null;
        }

        var duration = await GetBlurayClipDuration(
                files[0],
                mediaEncoder,
                memoryCache,
                cancellationToken)
            .ConfigureAwait(false);
        var frameRate = source.VideoStream?.AverageFrameRate ?? source.VideoStream?.RealFrameRate;

        // A single playlist entry may select only part of a clip. Do not serve the whole
        // file in that case. Allow one video frame for ffprobe/playlist duration rounding.
        if (
            duration is not > 0
            || frameRate is not > 0
            || !float.IsFinite(frameRate.Value)
            || Math.Abs((double)duration.Value - source.RunTimeTicks.Value)
                > TimeSpan.TicksPerSecond / frameRate.Value
        )
        {
            return null;
        }

        return GetStaticFileResult(files[0], "video/mp2t");
    }

    private static async Task<long?> GetBlurayClipDuration(
        string path,
        IMediaEncoder mediaEncoder,
        IMemoryCache memoryCache,
        CancellationToken cancellationToken)
    {
        // Serialize probes of the same clip, including concurrent HEAD and Range requests.
        using var acquired = await _blurayProbeLocks
            .LockAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var file = new FileInfo(path);
        var key = (typeof(FileStreamResponseHelpers), path);
        if (
            memoryCache.TryGetValue(key, out (long Length, DateTime Modified, long Duration) cached)
            && cached.Length == file.Length
            && cached.Modified == file.LastWriteTimeUtc
        )
        {
            return cached.Duration;
        }

        var info = await mediaEncoder
            .GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = DlnaProfileType.Video,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File,
                        VideoType = VideoType.VideoFile,
                    },
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (info.RunTimeTicks is > 0)
        {
            memoryCache.Set(key, (file.Length, file.LastWriteTimeUtc, info.RunTimeTicks.Value));
        }

        return info.RunTimeTicks;
    }

    /// <summary>
    /// Returns a transcoded file from the server.
    /// </summary>
    /// <param name="state">The current <see cref="StreamState"/>.</param>
    /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
    /// <param name="httpContext">The current http context.</param>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager"/> singleton.</param>
    /// <param name="ffmpegCommandLineArguments">The command line arguments to start ffmpeg.</param>
    /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
    /// <returns>A <see cref="Task{ActionResult}"/> containing the transcoded file.</returns>
    public static async Task<ActionResult> GetTranscodedFile(
        StreamState state,
        bool isHeadRequest,
        HttpContext httpContext,
        ITranscodeManager transcodeManager,
        string ffmpegCommandLineArguments,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource)
    {
        // Use the command line args with a dummy playlist path
        var outputPath = state.OutputFilePath;

        httpContext.Response.Headers[HeaderNames.AcceptRanges] = "none";

        var contentType = state.GetMimeType(outputPath);

        // Headers only
        if (isHeadRequest)
        {
            httpContext.Response.Headers[HeaderNames.ContentType] = contentType;
            return new OkResult();
        }

        using (await transcodeManager.LockAsync(outputPath, cancellationTokenSource.Token).ConfigureAwait(false))
        {
            TranscodingJob? job;
            if (!File.Exists(outputPath))
            {
                job = await transcodeManager.StartFfMpeg(
                    state,
                    outputPath,
                    ffmpegCommandLineArguments,
                    httpContext.User.GetUserId(),
                    transcodingJobType,
                    cancellationTokenSource).ConfigureAwait(false);
            }
            else
            {
                job = transcodeManager.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                state.Dispose();
            }

            var stream = new ProgressiveFileStream(outputPath, job, transcodeManager);
            return new FileStreamResult(stream, contentType);
        }
    }
}
