using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
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

        protected BaseProgressiveStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService, IFileSystem fileSystem, IItemRepository itemRepository, ILiveTvManager liveTvManager, IImageProcessor imageProcessor, IHttpClient httpClient)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, dtoService, fileSystem, itemRepository, liveTvManager)
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
            if (videoRequest != null && videoRequest.VideoCodec.HasValue)
            {
                if (state.IsInputVideo)
                {
                    switch (videoRequest.VideoCodec.Value)
                    {
                        case VideoCodecs.H264:
                            return ".ts";
                        case VideoCodecs.Theora:
                            return ".ogv";
                        case VideoCodecs.Vpx:
                            return ".webm";
                        case VideoCodecs.Wmv:
                            return ".asf";
                    }
                }
            }

            // Try to infer based on the desired audio codec
            if (state.Request.AudioCodec.HasValue)
            {
                if (!state.IsInputVideo)
                {
                    switch (state.Request.AudioCodec.Value)
                    {
                        case AudioCodecs.Aac:
                            return ".aac";
                        case AudioCodecs.Mp3:
                            return ".mp3";
                        case AudioCodecs.Vorbis:
                            return ".ogg";
                        case AudioCodecs.Wma:
                            return ".wma";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the dlna headers.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isStaticallyStreamed">if set to <c>true</c> [is statically streamed].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private void AddDlnaHeaders(StreamState state, IDictionary<string, string> responseHeaders, bool isStaticallyStreamed)
        {
            var timeSeek = GetHeader("TimeSeekRange.dlna.org");

            if (!string.IsNullOrEmpty(timeSeek))
            {
                ResultFactory.ThrowError(406, "Time seek not supported during encoding.", responseHeaders);
                return;
            }

            var transferMode = GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;
            responseHeaders["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            var contentFeatures = string.Empty;
            var extension = GetOutputFileExtension(state);

            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            var orgOp = isStaticallyStreamed ? ";DLNA.ORG_OP=01" : ";DLNA.ORG_OP=00";

            // 0 = native, 1 = transcoded
            var orgCi = isStaticallyStreamed ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            const string dlnaflags = ";DLNA.ORG_FLAGS=01500000000000000000000000000000";

            if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MP3";
            }
            else if (string.Equals(extension, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AAC_ISO";
            }
            else if (string.Equals(extension, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMABASE";
            }
            else if (string.Equals(extension, ".avi", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVI";
            }
            else if (string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MATROSKA";
            }
            else if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVC_MP4_MP_HD_720p_AAC";
            }
            //else if (string.Equals(extension, ".mpeg", StringComparison.OrdinalIgnoreCase))
            //{
            //    contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            //}
            //else if (string.Equals(extension, ".wmv", StringComparison.OrdinalIgnoreCase))
            //{
            //    contentFeatures = "DLNA.ORG_PN=WMVHIGH_BASE";
            //}
            //else if (string.Equals(extension, ".asf", StringComparison.OrdinalIgnoreCase))
            //{
            //    // ??
            //    contentFeatures = "DLNA.ORG_PN=WMVHIGH_BASE";
            //}


            if (!string.IsNullOrEmpty(contentFeatures))
            {
                responseHeaders["contentFeatures.dlna.org"] = (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
            }

            foreach (var item in responseHeaders)
            {
                Request.Response.AddHeader(item.Key, item.Value);
            }
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

            if (request.Static && state.IsRemote)
            {
                AddDlnaHeaders(state, responseHeaders, true);

                return GetStaticRemoteStreamResult(state.MediaPath, responseHeaders, isHeadRequest).Result;
            }

            var outputPath = GetOutputFilePath(state);
            var outputPathExists = File.Exists(outputPath);

            var isStatic = request.Static ||
                           (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive));

            AddDlnaHeaders(state, responseHeaders, isStatic);

            if (request.Static)
            {
                return ResultFactory.GetStaticFileResult(Request, state.MediaPath, FileShare.Read, responseHeaders, isHeadRequest);
            }

            if (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                return ResultFactory.GetStaticFileResult(Request, outputPath, FileShare.Read, responseHeaders, isHeadRequest);
            }

            return GetStreamResult(state, responseHeaders, isHeadRequest).Result;
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
            responseHeaders["Accept-Ranges"] = "none";

            var response = await HttpClient.GetResponse(new HttpRequestOptions
            {
                Url = mediaPath,
                UserAgent = GetUserAgent(mediaPath),
                BufferContent = false

            }).ConfigureAwait(false);


            if (isHeadRequest)
            {
                using (response.Content)
                {
                    return ResultFactory.GetResult(new MemoryStream(), response.ContentType, responseHeaders);
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

            var contentType = MimeTypes.GetMimeType(outputPath);

            // Headers only
            if (isHeadRequest)
            {
                responseHeaders["Accept-Ranges"] = "none";

                return ResultFactory.GetResult(null, contentType, responseHeaders);
            }

            if (!File.Exists(outputPath))
            {
                await StartFfMpeg(state, outputPath).ConfigureAwait(false);
            }
            else
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
            }

            var result = new ProgressiveStreamWriter(outputPath, Logger, FileSystem);

            result.Options["Accept-Ranges"] = "none";
            result.Options["Content-Type"] = contentType;

            // Add the response headers to the result object
            foreach (var item in responseHeaders)
            {
                result.Options[item.Key] = item.Value;
            }

            return result;
        }
    }
}
