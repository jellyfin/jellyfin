using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaybackDtos;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        /// <param name="httpClient">The <see cref="HttpClient"/> making the remote request.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
        public static async Task<ActionResult> GetStaticRemoteStreamResult(
            StreamState state,
            bool isHeadRequest,
            HttpClient httpClient,
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
            {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, useragent);
            }

            // Can't dispose the response as it's required up the call chain.
            var response = await httpClient.GetAsync(new Uri(state.MediaPath), cancellationToken).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.ToString();

            httpContext.Response.Headers[HeaderNames.AcceptRanges] = "none";

            if (isHeadRequest)
            {
                httpContext.Response.Headers[HeaderNames.ContentType] = contentType;
                return new OkResult();
            }

            return new FileStreamResult(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), contentType);
        }

        /// <summary>
        /// Returns a static file from the server.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <returns>An <see cref="ActionResult"/> the file.</returns>
        public static ActionResult GetStaticFileResult(
            string path,
            string contentType,
            bool isHeadRequest,
            HttpContext httpContext)
        {
            httpContext.Response.ContentType = contentType;

            // if the request is a head request, return an OkResult (200) with the same headers as it would with a GET request
            if (isHeadRequest)
            {
                return new OkResult();
            }

            // Handle symbolic links as source file:
            //
            // When replaying content via DLNA 'Microsoft.AspNetCore.Http.SendFileResponseExtensions.SendFileAsyncCore'
            // validates the length of the sent information. Unfortuanately the check fails for source files
            // which are symbolic links (aka ReparsePoint's). In this case the size of the filename of the referenced
            // file is reported instead of the size of the content of this file.
            //
            // * To fix this in AspNetCore.Http would be best, but is out of scope.
            // * Second best would be, to resolve the symbolic link target and return this as PhysicalFileResult. This
            //   would require to add Mono.Posix to this project, which would add another Assembly and which is - as far
            //   as I know - not portable to Windows and therefore would require conditional compilation.
            // * The third way is to use FileStreamResult for files known to be symbolic links, as it correctly reports
            //   the size of the open file. For all other files PhysicalFileResult is kept, as other parts of this
            //   project may rely on using it's properties.
            var fileInfo = System.IO.File.GetAttributes(path);
            if (fileInfo.HasFlag(FileAttributes.ReparsePoint))
            {
                var stream = File.OpenRead(path);
                return new FileStreamResult(stream, contentType) { EnableRangeProcessing = true };
            }

            return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
        }

        /// <summary>
        /// Returns a transcoded file from the server.
        /// </summary>
        /// <param name="state">The current <see cref="StreamState"/>.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <param name="transcodingJobHelper">The <see cref="TranscodingJobHelper"/> singleton.</param>
        /// <param name="ffmpegCommandLineArguments">The command line arguments to start ffmpeg.</param>
        /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the transcoded file.</returns>
        public static async Task<ActionResult> GetTranscodedFile(
            StreamState state,
            bool isHeadRequest,
            HttpContext httpContext,
            TranscodingJobHelper transcodingJobHelper,
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

            var transcodingLock = transcodingJobHelper.GetTranscodingLock(outputPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                TranscodingJobDto? job;
                if (!File.Exists(outputPath))
                {
                    job = await transcodingJobHelper.StartFfMpeg(state, outputPath, ffmpegCommandLineArguments, httpContext.Request, transcodingJobType, cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    job = transcodingJobHelper.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                    state.Dispose();
                }

                var stream = new ProgressiveFileStream(outputPath, job, transcodingJobHelper);
                return new FileStreamResult(stream, contentType);
            }
            finally
            {
                transcodingLock.Release();
            }
        }
    }
}
