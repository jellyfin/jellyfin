using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// The stream response helpers.
    /// </summary>
    public static class FileStreamResponseHelpers
    {
        /// <summary>
        /// Returns a static file from a remote source.
        /// </summary>
        /// <param name="state">The current <see cref="StreamState"/>.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="controller">The <see cref="ControllerBase"/> managing the response.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
        public static async Task<ActionResult> GetStaticRemoteStreamResult(
            StreamState state,
            bool isHeadRequest,
            ControllerBase controller,
            CancellationTokenSource cancellationTokenSource)
        {
            HttpClient httpClient = new HttpClient();
            var responseHeaders = controller.Response.Headers;

            if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
            {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, useragent);
            }

            var response = await httpClient.GetAsync(state.MediaPath).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType.ToString();

            responseHeaders[HeaderNames.AcceptRanges] = "none";

            // Seeing cases of -1 here
            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value >= 0)
            {
                responseHeaders[HeaderNames.ContentLength] = response.Content.Headers.ContentLength.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (isHeadRequest)
            {
                using (response)
                {
                    return controller.File(Array.Empty<byte>(), contentType);
                }
            }

            return controller.File(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), contentType);
        }

        /// <summary>
        /// Returns a static file from the server.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="dateLastModified">The <see cref="DateTime"/> of the last modification of the file.</param>
        /// <param name="cacheDuration">The cache duration of the file.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="controller">The <see cref="ControllerBase"/> managing the response.</param>
        /// <returns>An <see cref="ActionResult"/> the file.</returns>
        // TODO: caching doesn't work
        public static ActionResult GetStaticFileResult(
            string path,
            string contentType,
            DateTime dateLastModified,
            TimeSpan? cacheDuration,
            bool isHeadRequest,
            ControllerBase controller)
        {
            bool disableCaching = false;
            if (controller.Request.Headers.TryGetValue(HeaderNames.CacheControl, out StringValues headerValue))
            {
                disableCaching = headerValue.FirstOrDefault().Contains("no-cache", StringComparison.InvariantCulture);
            }

            bool parsingSuccessful = DateTime.TryParseExact(controller.Request.Headers[HeaderNames.IfModifiedSince], "ddd, dd MMM yyyy HH:mm:ss \"GMT\"", new CultureInfo("en-US", false), DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime ifModifiedSinceHeader);

            // if the parsing of the IfModifiedSince header was not successfull, disable caching
            if (!parsingSuccessful)
            {
                disableCaching = true;
            }

            controller.Response.ContentType = contentType;
            controller.Response.Headers.Add(HeaderNames.Age, Convert.ToInt64((DateTime.UtcNow - dateLastModified).TotalSeconds).ToString(CultureInfo.InvariantCulture));
            controller.Response.Headers.Add(HeaderNames.Vary, HeaderNames.Accept);

            if (disableCaching)
            {
                controller.Response.Headers.Add(HeaderNames.CacheControl, "no-cache, no-store, must-revalidate");
                controller.Response.Headers.Add(HeaderNames.Pragma, "no-cache, no-store, must-revalidate");
            }
            else
            {
                if (cacheDuration.HasValue)
                {
                    controller.Response.Headers.Add(HeaderNames.CacheControl, "public, max-age=" + cacheDuration.Value.TotalSeconds);
                }
                else
                {
                    controller.Response.Headers.Add(HeaderNames.CacheControl, "public");
                }

                controller.Response.Headers.Add(HeaderNames.LastModified, dateLastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss \"GMT\"", new CultureInfo("en-US", false)));

                // if the image was not modified since "ifModifiedSinceHeader"-header, return a HTTP status code 304 not modified
                if (!(dateLastModified > ifModifiedSinceHeader))
                {
                    if (ifModifiedSinceHeader.Add(cacheDuration!.Value) < DateTime.UtcNow)
                    {
                        controller.Response.StatusCode = StatusCodes.Status304NotModified;
                        return new ContentResult();
                    }
                }
            }

            // if the request is a head request, return a NoContent result with the same headers as it would with a GET request
            if (isHeadRequest)
            {
                return controller.NoContent();
            }

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return controller.File(stream, contentType);
        }

        /// <summary>
        /// Returns a transcoded file from the server.
        /// </summary>
        /// <param name="state">The current <see cref="StreamState"/>.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="streamHelper">Instance of the <see cref="IStreamHelper"/> interface.</param>
        /// <param name="controller">The <see cref="ControllerBase"/> managing the response.</param>
        /// <param name="transcodingJobHelper">The <see cref="TranscodingJobHelper"/> singleton.</param>
        /// <param name="ffmpegCommandLineArguments">The command line arguments to start ffmpeg.</param>
        /// <param name="request">The <see cref="HttpRequest"/> starting the transcoding.</param>
        /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the transcoded file.</returns>
        public static async Task<ActionResult> GetTranscodedFile(
            StreamState state,
            bool isHeadRequest,
            IStreamHelper streamHelper,
            ControllerBase controller,
            TranscodingJobHelper transcodingJobHelper,
            string ffmpegCommandLineArguments,
            HttpRequest request,
            TranscodingJobType transcodingJobType,
            CancellationTokenSource cancellationTokenSource)
        {
            IHeaderDictionary responseHeaders = controller.Response.Headers;
            // Use the command line args with a dummy playlist path
            var outputPath = state.OutputFilePath;

            responseHeaders[HeaderNames.AcceptRanges] = "none";

            var contentType = state.GetMimeType(outputPath);

            // TODO: The isHeadRequest is only here because ServiceStack will add Content-Length=0 to the response
            // TODO (from api-migration): Investigate if this is still neccessary as we migrated away from ServiceStack
            var contentLength = state.EstimateContentLength || isHeadRequest ? GetEstimatedContentLength(state) : null;

            if (contentLength.HasValue)
            {
                responseHeaders[HeaderNames.ContentLength] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                responseHeaders.Remove(HeaderNames.ContentLength);
            }

            // Headers only
            if (isHeadRequest)
            {
                return controller.File(Array.Empty<byte>(), contentType);
            }

            var transcodingLock = transcodingJobHelper.GetTranscodingLock(outputPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                if (!File.Exists(outputPath))
                {
                    await transcodingJobHelper.StartFfMpeg(state, outputPath, ffmpegCommandLineArguments, request, transcodingJobType, cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    transcodingJobHelper.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                    state.Dispose();
                }

                Stream stream = new MemoryStream();

                await new ProgressiveFileCopier(streamHelper, outputPath).WriteToAsync(stream, CancellationToken.None).ConfigureAwait(false);
                return controller.File(stream, contentType);
            }
            finally
            {
                transcodingLock.Release();
            }
        }

        /// <summary>
        /// Gets the length of the estimated content.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        private static long? GetEstimatedContentLength(StreamState state)
        {
            var totalBitrate = state.TotalOutputBitrate ?? 0;

            if (totalBitrate > 0 && state.RunTimeTicks.HasValue)
            {
                return Convert.ToInt64(totalBitrate * TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds / 8);
            }

            return null;
        }
    }
}
