using MediaBrowser.Api.Images;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class BaseProgressiveStreamingService
    /// </summary>
    public abstract class BaseProgressiveStreamingService : BaseStreamingService
    {
        protected readonly IItemRepository ItemRepository;
        protected readonly IImageProcessor ImageProcessor;

        protected BaseProgressiveStreamingService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepository, IDtoService dtoService, IImageProcessor imageProcessor, IFileSystem fileSystem) :
            base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, dtoService, fileSystem)
        {
            ItemRepository = itemRepository;
            ImageProcessor = imageProcessor;
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
                var video = state.Item as Video;

                if (video != null)
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
                var audio = state.Item as Audio;

                if (audio != null)
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
            var timeSeek = RequestContext.GetHeader("TimeSeekRange.dlna.org");

            if (!string.IsNullOrEmpty(timeSeek))
            {
                ResultFactory.ThrowError(406, "Time seek not supported during encoding.", responseHeaders);
                return;
            }

            var transferMode = RequestContext.GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;

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
            else if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG4_P2_SP_AAC";
            }
            else if (string.Equals(extension, ".mpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            }
            else if (string.Equals(extension, ".wmv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVHIGH_BASE";
            }
            else if (string.Equals(extension, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                // ??
                contentFeatures = "DLNA.ORG_PN=WMVHIGH_BASE";
            }
            else if (string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase))
            {
                // ??
                contentFeatures = "";
            }

            if (!string.IsNullOrEmpty(contentFeatures))
            {
                responseHeaders["ContentFeatures.DLNA.ORG"] = (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
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
            var state = GetState(request);

            if (request.AlbumArt)
            {
                return GetAlbumArtResponse(state);
            }

            var responseHeaders = new Dictionary<string, string>();

            if (request.Static && state.Item.LocationType == LocationType.Remote)
            {
                return GetStaticRemoteStreamResult(state.Item, responseHeaders, isHeadRequest).Result;
            }

            var outputPath = GetOutputFilePath(state);
            var outputPathExists = File.Exists(outputPath);

            //var isStatic = request.Static ||
            //               (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive));

            //AddDlnaHeaders(state, responseHeaders, isStatic);

            if (request.Static)
            {
                return ResultFactory.GetStaticFileResult(RequestContext, state.Item.Path, FileShare.Read, responseHeaders, isHeadRequest);
            }

            if (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                return ResultFactory.GetStaticFileResult(RequestContext, outputPath, FileShare.Read, responseHeaders, isHeadRequest);
            }

            return GetStreamResult(state, responseHeaders, isHeadRequest).Result;
        }

        /// <summary>
        /// Gets the static remote stream result.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStaticRemoteStreamResult(BaseItem item, Dictionary<string, string> responseHeaders, bool isHeadRequest)
        {
            responseHeaders["Accept-Ranges"] = "none";

            var httpClient = new HttpClient();

            using (var message = new HttpRequestMessage(HttpMethod.Get, item.Path))
            {
                var useragent = GetUserAgent(item);

                if (!string.IsNullOrEmpty(useragent))
                {
                    message.Headers.Add("User-Agent", useragent);
                }

                var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var contentType = response.Content.Headers.ContentType.MediaType;

                // Headers only
                if (isHeadRequest)
                {
                    response.Dispose();
                    httpClient.Dispose();

                    return ResultFactory.GetResult(null, contentType, responseHeaders);
                }

                var result = new StaticRemoteStreamWriter(response, httpClient);

                result.Options["Content-Type"] = contentType;

                // Add the response headers to the result object
                foreach (var header in responseHeaders)
                {
                    result.Options[header.Key] = header.Value;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the album art response.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.Object.</returns>
        private object GetAlbumArtResponse(StreamState state)
        {
            var request = new GetItemImage
            {
                MaxWidth = 800,
                MaxHeight = 800,
                Type = ImageType.Primary,
                Id = state.Item.Id.ToString()
            };

            // Try and find some image to return
            if (!state.Item.HasImage(ImageType.Primary))
            {
                if (state.Item.HasImage(ImageType.Backdrop))
                {
                    request.Type = ImageType.Backdrop;
                }
                else if (state.Item.HasImage(ImageType.Thumb))
                {
                    request.Type = ImageType.Thumb;
                }
                else if (state.Item.HasImage(ImageType.Logo))
                {
                    request.Type = ImageType.Logo;
                }
            }

            return new ImageService(UserManager, LibraryManager, ApplicationPaths, null, ItemRepository, DtoService, ImageProcessor)
            {
                Logger = Logger,
                RequestContext = RequestContext,
                ResultFactory = ResultFactory

            }.Get(request);
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
