using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class BaseProgressiveStreamingService
    /// </summary>
    public abstract class BaseProgressiveStreamingService : BaseStreamingService
    {
        protected readonly IImageProcessor ImageProcessor;
        protected readonly IHttpClient HttpClient;

        protected BaseProgressiveStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService, IFileSystem fileSystem, IItemRepository itemRepository, ILiveTvManager liveTvManager, IEncodingManager encodingManager, IDlnaManager dlnaManager, IChannelManager channelManager, IImageProcessor imageProcessor, IHttpClient httpClient) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, dtoService, fileSystem, itemRepository, liveTvManager, encodingManager, dlnaManager, channelManager)
        {
            ImageProcessor = imageProcessor;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the output file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetOutputFileExtension(StreamState state)
        {
            var ext = base.GetOutputFileExtension(state);

            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }

            var videoRequest = state.Request as VideoStreamRequest;

            // Try to infer based on the desired video codec
            if (videoRequest != null && !string.IsNullOrEmpty(videoRequest.VideoCodec))
            {
                if (state.IsInputVideo)
                {
                    if (string.Equals(videoRequest.VideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".ts";
                    }
                    if (string.Equals(videoRequest.VideoCodec, "theora", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".ogv";
                    }
                    if (string.Equals(videoRequest.VideoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".webm";
                    }
                    if (string.Equals(videoRequest.VideoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".asf";
                    }
                }
            }

            // Try to infer based on the desired audio codec
            if (!string.IsNullOrEmpty(state.Request.AudioCodec))
            {
                if (!state.IsInputVideo)
                {
                    if (string.Equals("aac", state.Request.AudioCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return ".aac";
                    }
                    if (string.Equals("mp3", state.Request.AudioCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return ".mp3";
                    }
                    if (string.Equals("vorbis", state.Request.AudioCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return ".ogg";
                    }
                    if (string.Equals("wma", state.Request.AudioCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return ".wma";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType
        {
            get { return TranscodingJobType.Progressive; }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task.</returns>
        protected object ProcessRequest(StreamRequest request, bool isHeadRequest)
        {
            var state = GetState(request, CancellationToken.None).Result;

            var responseHeaders = new Dictionary<string, string>();

            // Static remote stream
            if (request.Static && state.IsRemote)
            {
                AddDlnaHeaders(state, responseHeaders, true);

                try
                {
                    return GetStaticRemoteStreamResult(state.MediaPath, responseHeaders, isHeadRequest).Result;
                }
                finally
                {
                    state.Dispose();
                }
            }

            var outputPath = GetOutputFilePath(state);
            var outputPathExists = File.Exists(outputPath);

            var isStatic = request.Static ||
                           (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive));

            AddDlnaHeaders(state, responseHeaders, isStatic);

            // Static stream
            if (request.Static)
            {
                var contentType = state.GetMimeType(state.MediaPath);

                try
                {
                    return ResultFactory.GetStaticFileResult(Request, state.MediaPath, contentType, FileShare.Read, responseHeaders, isHeadRequest);
                }
                finally
                {
                    state.Dispose();
                }
            }

            // Not static but transcode cache file exists
            if (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                var contentType = state.GetMimeType(outputPath);

                try
                {
                    return ResultFactory.GetStaticFileResult(Request, outputPath, contentType, FileShare.Read, responseHeaders, isHeadRequest);
                }
                finally
                {
                    state.Dispose();
                }
            }

            // Need to start ffmpeg
            try
            {
                return GetStreamResult(state, responseHeaders, isHeadRequest).Result;
            }
            catch
            {
                state.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Gets the static remote stream result.
        /// </summary>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStaticRemoteStreamResult(string mediaPath, Dictionary<string, string> responseHeaders, bool isHeadRequest)
        {
            var options = new HttpRequestOptions
            {
                Url = mediaPath,
                UserAgent = GetUserAgent(mediaPath),
                BufferContent = false
            };

            var response = await HttpClient.GetResponse(options).ConfigureAwait(false);

            responseHeaders["Accept-Ranges"] = "none";

            var length = response.Headers["Content-Length"];

            if (!string.IsNullOrEmpty(length))
            {
                responseHeaders["Content-Length"] = length;
            }

            if (isHeadRequest)
            {
                using (response.Content)
                {
                    return ResultFactory.GetResult(new byte[] { }, response.ContentType, responseHeaders);
                }
            }

            var result = new StaticRemoteStreamWriter(response);

            result.Options["Content-Type"] = response.ContentType;

            // Add the response headers to the result object
            foreach (var header in responseHeaders)
            {
                result.Options[header.Key] = header.Value;
            }

            return result;
        }

        /// <summary>
        /// Gets the stream result.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStreamResult(StreamState state, IDictionary<string, string> responseHeaders, bool isHeadRequest)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = GetOutputFilePath(state);

            responseHeaders["Accept-Ranges"] = "none";

            var contentType = state.GetMimeType(outputPath);

            var contentLength = state.EstimateContentLength ? GetEstimatedContentLength(state) : null;

            if (contentLength.HasValue)
            {
                responseHeaders["Content-Length"] = contentLength.Value.ToString(UsCulture);
            }

            // Headers only
            if (isHeadRequest)
            {
                var streamResult = ResultFactory.GetResult(new byte[] { }, contentType, responseHeaders);

                if (!contentLength.HasValue)
                {
                    var hasOptions = streamResult as IHasOptions;
                    if (hasOptions != null)
                    {
                        if (hasOptions.Options.ContainsKey("Content-Length"))
                        {
                            hasOptions.Options.Remove("Content-Length");
                        }
                    }
                }

                return streamResult;
            }

            if (!File.Exists(outputPath))
            {
                await StartFfMpeg(state, outputPath).ConfigureAwait(false);
            }
            else
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                state.Dispose();
            }

            var result = new ProgressiveStreamWriter(outputPath, Logger, FileSystem);

            result.Options["Content-Type"] = contentType;

            // Add the response headers to the result object
            foreach (var item in responseHeaders)
            {
                result.Options[item.Key] = item.Value;
            }

            return result;
        }

        /// <summary>
        /// Gets the length of the estimated content.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        private long? GetEstimatedContentLength(StreamState state)
        {
            var totalBitrate = state.TotalOutputBitrate ?? 0;

            if (totalBitrate > 0 && state.RunTimeTicks.HasValue)
            {
                return Convert.ToInt64(totalBitrate * TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds);
            }

            return null;
        }
    }
}
