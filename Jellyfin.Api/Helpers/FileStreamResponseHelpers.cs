using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
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
        /// <param name="controller">The <see cref="ControllerBase"/> managing the response.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> making the remote request.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
        public static async Task<ActionResult> GetStaticRemoteStreamResult(
            StreamState state,
            bool isHeadRequest,
            ControllerBase controller,
            HttpClient httpClient)
        {
            if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
            {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, useragent);
            }

            var response = await httpClient.GetAsync(state.MediaPath).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType.ToString();

            controller.Response.Headers[HeaderNames.AcceptRanges] = "none";

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
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="controller">The <see cref="ControllerBase"/> managing the response.</param>
        /// <returns>An <see cref="ActionResult"/> the file.</returns>
        public static ActionResult GetStaticFileResult(
            string path,
            string contentType,
            bool isHeadRequest,
            ControllerBase controller)
        {
            controller.Response.ContentType = contentType;

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
            // Use the command line args with a dummy playlist path
            var outputPath = state.OutputFilePath;

            controller.Response.Headers[HeaderNames.AcceptRanges] = "none";

            var contentType = state.GetMimeType(outputPath);

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

                using (var memoryStream = new MemoryStream())
                {
                    await new ProgressiveFileCopier(streamHelper, outputPath).WriteToAsync(memoryStream, CancellationToken.None).ConfigureAwait(false);
                    return controller.File(memoryStream, contentType);
                }
            }
            finally
            {
                transcodingLock.Release();
            }
        }
    }
}
