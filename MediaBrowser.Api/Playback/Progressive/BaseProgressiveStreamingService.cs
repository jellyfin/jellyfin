using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class BaseProgressiveStreamingService
    /// </summary>
    public abstract class BaseProgressiveStreamingService : BaseStreamingService
    {
        protected IHttpClient HttpClient { get; private set; }

        public BaseProgressiveStreamingService(
            ILogger<BaseProgressiveStreamingService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IHttpClient httpClient,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IJsonSerializer jsonSerializer,
            IAuthorizationContext authorizationContext,
            EncodingHelper encodingHelper)
            : base(
                logger,
                serverConfigurationManager,
                httpResultFactory,
                userManager,
                libraryManager,
                isoManager,
                mediaEncoder,
                fileSystem,
                dlnaManager,
                deviceManager,
                mediaSourceManager,
                jsonSerializer,
                authorizationContext,
                encodingHelper)
        {
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

                if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoCodec, "h265", StringComparison.OrdinalIgnoreCase))
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
        protected override TranscodingJobType TranscodingJobType => TranscodingJobType.Progressive;

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task.</returns>
        protected async Task<object> ProcessRequest(StreamRequest request, bool isHeadRequest)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var state = await GetState(request, cancellationTokenSource.Token).ConfigureAwait(false);

            var responseHeaders = new Dictionary<string, string>();

            if (request.Static && state.DirectStreamProvider != null)
            {
                AddDlnaHeaders(state, responseHeaders, true);

                using (state)
                {
                    var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // TODO: Don't hardcode this
                    outputHeaders[HeaderNames.ContentType] = Model.Net.MimeTypes.GetMimeType("file.ts");

                    return new ProgressiveFileCopier(state.DirectStreamProvider, outputHeaders, null, Logger, CancellationToken.None)
                    {
                        AllowEndOfFile = false
                    };
                }
            }

            // Static remote stream
            if (request.Static && state.InputProtocol == MediaProtocol.Http)
            {
                AddDlnaHeaders(state, responseHeaders, true);

                using (state)
                {
                    return await GetStaticRemoteStreamResult(state, responseHeaders, isHeadRequest, cancellationTokenSource).ConfigureAwait(false);
                }
            }

            if (request.Static && state.InputProtocol != MediaProtocol.File)
            {
                throw new ArgumentException(string.Format("Input protocol {0} cannot be streamed statically.", state.InputProtocol));
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = File.Exists(outputPath);

            var transcodingJob = ApiEntryPoint.Instance.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
            var isTranscodeCached = outputPathExists && transcodingJob != null;

            AddDlnaHeaders(state, responseHeaders, request.Static || isTranscodeCached);

            // Static stream
            if (request.Static)
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                using (state)
                {
                    if (state.MediaSource.IsInfiniteStream)
                    {
                        var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [HeaderNames.ContentType] = contentType
                        };


                        return new ProgressiveFileCopier(FileSystem, state.MediaPath, outputHeaders, null, Logger, CancellationToken.None)
                        {
                            AllowEndOfFile = false
                        };
                    }

                    TimeSpan? cacheDuration = null;

                    if (!string.IsNullOrEmpty(request.Tag))
                    {
                        cacheDuration = TimeSpan.FromDays(365);
                    }

                    return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
                    {
                        ResponseHeaders = responseHeaders,
                        ContentType = contentType,
                        IsHeadRequest = isHeadRequest,
                        Path = state.MediaPath,
                        CacheDuration = cacheDuration

                    }).ConfigureAwait(false);
                }
            }

            //// Not static but transcode cache file exists
            //if (isTranscodeCached && state.VideoRequest == null)
            //{
            //    var contentType = state.GetMimeType(outputPath);

            //    try
            //    {
            //        if (transcodingJob != null)
            //        {
            //            ApiEntryPoint.Instance.OnTranscodeBeginRequest(transcodingJob);
            //        }

            //        return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            //        {
            //            ResponseHeaders = responseHeaders,
            //            ContentType = contentType,
            //            IsHeadRequest = isHeadRequest,
            //            Path = outputPath,
            //            FileShare = FileShare.ReadWrite,
            //            OnComplete = () =>
            //            {
            //                if (transcodingJob != null)
            //                {
            //                    ApiEntryPoint.Instance.OnTranscodeEndRequest(transcodingJob);
            //                }
            //            }

            //        }).ConfigureAwait(false);
            //    }
            //    finally
            //    {
            //        state.Dispose();
            //    }
            //}

            // Need to start ffmpeg
            try
            {
                return await GetStreamResult(request, state, responseHeaders, isHeadRequest, cancellationTokenSource).ConfigureAwait(false);
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
        private async Task<object> GetStaticRemoteStreamResult(
            StreamState state,
            Dictionary<string, string> responseHeaders,
            bool isHeadRequest,
            CancellationTokenSource cancellationTokenSource)
        {
            var options = new HttpRequestOptions
            {
                Url = state.MediaPath,
                BufferContent = false,
                CancellationToken = cancellationTokenSource.Token
            };

            if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
            {
                options.UserAgent = useragent;
            }

            var response = await HttpClient.GetResponse(options).ConfigureAwait(false);

            responseHeaders[HeaderNames.AcceptRanges] = "none";

            // Seeing cases of -1 here
            if (response.ContentLength.HasValue && response.ContentLength.Value >= 0)
            {
                responseHeaders[HeaderNames.ContentLength] = response.ContentLength.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (isHeadRequest)
            {
                using (response)
                {
                    return ResultFactory.GetResult(null, Array.Empty<byte>(), response.ContentType, responseHeaders);
                }
            }

            var result = new StaticRemoteStreamWriter(response);

            result.Headers[HeaderNames.ContentType] = response.ContentType;

            // Add the response headers to the result object
            foreach (var header in responseHeaders)
            {
                result.Headers[header.Key] = header.Value;
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
        private async Task<object> GetStreamResult(StreamRequest request, StreamState state, IDictionary<string, string> responseHeaders, bool isHeadRequest, CancellationTokenSource cancellationTokenSource)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = state.OutputFilePath;

            responseHeaders[HeaderNames.AcceptRanges] = "none";

            var contentType = state.GetMimeType(outputPath);

            // TODO: The isHeadRequest is only here because ServiceStack will add Content-Length=0 to the response
            var contentLength = state.EstimateContentLength || isHeadRequest ? GetEstimatedContentLength(state) : null;

            if (contentLength.HasValue)
            {
                responseHeaders[HeaderNames.ContentLength] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
            }

            // Headers only
            if (isHeadRequest)
            {
                var streamResult = ResultFactory.GetResult(null, Array.Empty<byte>(), contentType, responseHeaders);

                if (streamResult is IHasHeaders hasHeaders)
                {
                    if (contentLength.HasValue)
                    {
                        hasHeaders.Headers[HeaderNames.ContentLength] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        hasHeaders.Headers.Remove(HeaderNames.ContentLength);
                    }
                }

                return streamResult;
            }

            var transcodingLock = ApiEntryPoint.Instance.GetTranscodingLock(outputPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                TranscodingJob job;

                if (!File.Exists(outputPath))
                {
                    job = await StartFfMpeg(state, outputPath, cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                    state.Dispose();
                }

                var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [HeaderNames.ContentType] = contentType
                };


                // Add the response headers to the result object
                foreach (var item in responseHeaders)
                {
                    outputHeaders[item.Key] = item.Value;
                }

                return new ProgressiveFileCopier(FileSystem, outputPath, outputHeaders, job, Logger, CancellationToken.None);
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
        private long? GetEstimatedContentLength(StreamState state)
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
