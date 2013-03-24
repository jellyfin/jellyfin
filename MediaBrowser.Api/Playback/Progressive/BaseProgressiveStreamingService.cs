using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class BaseProgressiveStreamingService
    /// </summary>
    public abstract class BaseProgressiveStreamingService : BaseStreamingService
    {
        protected BaseProgressiveStreamingService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager) :
            base(appPaths, userManager, libraryManager, isoManager)
        {
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
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private void AddDlnaHeaders(StreamState state, IDictionary<string,string> responseHeaders)
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

            if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".avi", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVI;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG4_P2_SP_AAC;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".mpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".wmv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVHIGH_BASE;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000";
            }
            else if (string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_OP=01;DLNA.ORG_CI=0";
            }

            if (!string.IsNullOrEmpty(contentFeatures))
            {
                responseHeaders["ContentFeatures.DLNA.ORG"] = contentFeatures;
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

            var responseHeaders = new Dictionary<string, string>();

            AddDlnaHeaders(state, responseHeaders);

            if (request.Static)
            {
                return ResultFactory.GetStaticFileResult(RequestContext, state.Item.Path, responseHeaders, isHeadRequest);
            }

            var outputPath = GetOutputFilePath(state);

            if (File.Exists(outputPath) && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                return ResultFactory.GetStaticFileResult(RequestContext, outputPath, responseHeaders, isHeadRequest);
            }

            return GetStreamResult(state, responseHeaders, isHeadRequest).Result;
        }

        /// <summary>
        /// Gets the stream result.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetStreamResult(StreamState state, IDictionary<string,string> responseHeaders, bool isHeadRequest)
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
                await StartFFMpeg(state, outputPath).ConfigureAwait(false);
            }
            else
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
            }

            var result = new ProgressiveStreamWriter(outputPath, state, Logger);

            result.Options["Accept-Ranges"] = "none";
            result.Options["Content-Type"] = contentType;

            // Add the response headers to the result object
            foreach (var item in responseHeaders)
            {
                result.Options[item.Key] = item.Value;
            }

            return result;
        }

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        protected override void DeletePartialStreamFiles(string outputFilePath)
        {
            File.Delete(outputFilePath);
        }
    }
}
