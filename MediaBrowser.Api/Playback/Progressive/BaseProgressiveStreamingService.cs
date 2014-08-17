using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
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

        protected BaseProgressiveStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, IImageProcessor imageProcessor, IHttpClient httpClient) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
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

            var isVideoRequest = state.VideoRequest != null;

            // Try to infer based on the desired video codec
            if (isVideoRequest)
            {
                var videoCodec = state.VideoRequest.VideoCodec;
                
                    if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".ts";
                    }
                    if (string.Equals(videoCodec, "theora", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".ogv";
                    }
                    if (string.Equals(videoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".webm";
                    }
                    if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".asf";
                    }
            }

            // Try to infer based on the desired audio codec
            if (!isVideoRequest)
            {
                var audioCodec = state.Request.AudioCodec;

                if (string.Equals("aac", audioCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return ".aac";
                }
                if (string.Equals("mp3", audioCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return ".mp3";
                }
                if (string.Equals("vorbis", audioCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return ".ogg";
                }
                if (string.Equals("wma", audioCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return ".wma";
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
            var cancellationTokenSource = new CancellationTokenSource();

            var state = GetState(request, cancellationTokenSource.Token).Result;

            var responseHeaders = new Dictionary<string, string>();

            // Static remote stream
            if (request.Static && state.InputProtocol == MediaProtocol.Http)
            {
                AddDlnaHeaders(state, responseHeaders, true);

                using (state)
                {
                    return GetStaticRemoteStreamResult(state, responseHeaders, isHeadRequest, cancellationTokenSource).Result;
                }
            }

            if (request.Static && state.InputProtocol != MediaProtocol.File)
            {
                throw new ArgumentException(string.Format("Input protocol {0} cannot be streamed statically.", state.InputProtocol));
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = File.Exists(outputPath);

            var isStatic = request.Static ||
                           (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive));

            AddDlnaHeaders(state, responseHeaders, isStatic);

            // Static stream
            if (request.Static)
            {
                var contentType = state.GetMimeType(state.MediaPath);

                using (state)
                {
                    return ResultFactory.GetStaticFileResult(Request, state.MediaPath, contentType, null, FileShare.Read, responseHeaders, isHeadRequest);
                }
            }

            // Not static but transcode cache file exists
            if (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                var contentType = state.GetMimeType(outputPath);

                try
                {
                    return ResultFactory.GetStaticFileResult(Request, outputPath, contentType, null, FileShare.Read, responseHeaders, isHeadRequest);
                }
                finally
                {
                    state.Dispose();
                }
            }

            // Need to start ffmpeg
            try
            {
                return GetStreamResult(state, responseHeaders, isHeadRequest, cancellationTokenSource).Result;
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
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStaticRemoteStreamResult(StreamState state, Dictionary<string, string> responseHeaders, bool isHeadRequest, CancellationTokenSource cancellationTokenSource)
        {
            string useragent = null;
            state.RemoteHttpHeaders.TryGetValue("User-Agent", out useragent);

            var options = new HttpRequestOptions
            {
                Url = state.MediaPath,
                UserAgent = useragent,
                BufferContent = false,
                CancellationToken = cancellationTokenSource.Token
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
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStreamResult(StreamState state, IDictionary<string, string> responseHeaders, bool isHeadRequest, CancellationTokenSource cancellationTokenSource)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = state.OutputFilePath;

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

            await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                if (!File.Exists(outputPath))
                {
                    await StartFfMpeg(state, outputPath, cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                    state.Dispose();
                }

                var job = ApiEntryPoint.Instance.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
                var result = new ProgressiveStreamWriter(outputPath, Logger, FileSystem, job);

                result.Options["Content-Type"] = contentType;

                // Add the response headers to the result object
                foreach (var item in responseHeaders)
                {
                    result.Options[item.Key] = item.Value;
                }

                return result;
            }
            finally
            {
                ApiEntryPoint.Instance.TranscodingStartLock.Release();
            }
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
