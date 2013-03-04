using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using System;
using MediaBrowser.Controller.Library;
using ServiceStack.ServiceHost;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class GetAudioStream
    /// </summary>
    [Route("/Videos/{Id}/stream.ts", "GET")]
    [Route("/Videos/{Id}/stream.webm", "GET")]
    [Route("/Videos/{Id}/stream.asf", "GET")]
    [Route("/Videos/{Id}/stream.wmv", "GET")]
    [Route("/Videos/{Id}/stream.ogv", "GET")]
    [Route("/Videos/{Id}/stream.mp4", "GET")]
    [Route("/Videos/{Id}/stream.m4v", "GET")]
    [Route("/Videos/{Id}/stream.mkv", "GET")]
    [Route("/Videos/{Id}/stream.mpeg", "GET")]
    [Route("/Videos/{Id}/stream.avi", "GET")]
    [Route("/Videos/{Id}/stream", "GET")]
    public class GetVideoStream : StreamRequest
    {

    }
    
    /// <summary>
    /// Class VideoService
    /// </summary>
    public class VideoService : BaseProgressiveStreamingService
    {
        public VideoService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager) 
            : base(appPaths, userManager, libraryManager, isoManager)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVideoStream request)
        {
            return ProcessRequest(request);
        }
        
        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state)
        {
            var video = (Video)state.Item;

            var probeSize = Kernel.Instance.FFMpegManager.GetProbeSizeArgument(video.VideoType, video.IsoType);

            // Get the output codec name
            var videoCodec = GetVideoCodec(state.Request);

            var graphicalSubtitleParam = string.Empty;

            if (state.SubtitleStream != null)
            {
                // This is for internal graphical subs
                if (!state.SubtitleStream.IsExternal && (state.SubtitleStream.Codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) != -1 || state.SubtitleStream.Codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    graphicalSubtitleParam = GetInternalGraphicalSubtitleParam(state, videoCodec);
                }
            }

            return string.Format("{0} {1} -i {2}{3} -threads 0 {4} {5}{6} {7} \"{8}\"",
                probeSize,
                GetFastSeekCommandLineParameter(state.Request),
                GetInputArgument(video, state.IsoMount),
                GetSlowSeekCommandLineParameter(state.Request),
                GetMapArgs(state),
                GetVideoArguments(state, videoCodec),
                graphicalSubtitleParam,
                GetAudioArguments(state),
                outputPath
                ).Trim();
        }

        /// <summary>
        /// Gets video arguments to pass to ffmpeg
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoArguments(StreamState state, string videoCodec)
        {
            var args = "-vcodec " + videoCodec;

            var request = state.Request;

            // If we're encoding video, add additional params
            if (!videoCodec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add resolution params, if specified
                if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
                {
                    args += GetOutputSizeParam(state, videoCodec);
                }

                if (request.Framerate.HasValue)
                {
                    args += string.Format(" -r {0}", request.Framerate.Value);
                }

                // Add the audio bitrate
                var qualityParam = GetVideoQualityParam(request, videoCodec);

                if (!string.IsNullOrEmpty(qualityParam))
                {
                    args += " " + qualityParam;
                }
            }
            else if (IsH264(state.VideoStream))
            {
                args += " -bsf h264_mp4toannexb";
            }

            return args;
        }

        /// <summary>
        /// Gets audio arguments to pass to ffmpeg
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetAudioArguments(StreamState state)
        {
            // If the video doesn't have an audio stream, return a default.
            if (state.AudioStream == null)
            {
                return string.Empty;
            }

            var request = state.Request;

            // Get the output codec name
            var codec = GetAudioCodec(request);

            var args = "-acodec " + codec;

            // If we're encoding audio, add additional params
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add the number of audio channels
                var channels = GetNumAudioChannelsParam(request, state.AudioStream);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                if (request.AudioSampleRate.HasValue)
                {
                    args += " -ar " + request.AudioSampleRate.Value;
                }

                if (request.AudioBitRate.HasValue)
                {
                    args += " -ab " + request.AudioBitRate.Value;
                }
            }

            return args;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoQualityParam(StreamRequest request, string videoCodec)
        {
            var args = string.Empty;

            // webm
            if (videoCodec.Equals("libvpx", StringComparison.OrdinalIgnoreCase))
            {
                args = "-g 120 -cpu-used 1 -lag-in-frames 16 -deadline realtime -slices 4 -vprofile 0";
            }

            // asf/wmv
            else if (videoCodec.Equals("wmv2", StringComparison.OrdinalIgnoreCase))
            {
                args = "-g 100 -qmax 15";
            }

            else if (videoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                args = "-preset superfast";
            }

            if (request.VideoBitRate.HasValue)
            {
                args += " -b:v " + request.VideoBitRate;
            }

            return args.Trim();
        }
    }
}
