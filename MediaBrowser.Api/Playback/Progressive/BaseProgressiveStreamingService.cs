using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
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

            if (request.Static)
            {
                return ToStaticFileResult(state.Item.Path, isHeadRequest);
            }

            var outputPath = GetOutputFilePath(state);

            if (File.Exists(outputPath) && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                return ToStaticFileResult(outputPath, isHeadRequest);
            }

            return GetStreamResult(state, isHeadRequest).Result;
        }

        /// <summary>
        /// Gets the stream result.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<ProgressiveStreamWriter> GetStreamResult(StreamState state, bool isHeadRequest)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = GetOutputFilePath(state);

            Response.ContentType = MimeTypes.GetMimeType(outputPath);

            // Headers only
            if (isHeadRequest)
            {
                return null;
            }

            if (!File.Exists(outputPath))
            {
                await StartFFMpeg(state, outputPath).ConfigureAwait(false);
            }
            else
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
            }

            return new ProgressiveStreamWriter(outputPath, state, Logger);
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
