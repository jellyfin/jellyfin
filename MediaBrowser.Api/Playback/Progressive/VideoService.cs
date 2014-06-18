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
using ServiceStack;
using System;
using System.IO;
using System.Threading;

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
    [Route("/Videos/{Id}/stream.mpg", "GET")]
    [Route("/Videos/{Id}/stream.avi", "GET")]
    [Route("/Videos/{Id}/stream.m2ts", "GET")]
    [Route("/Videos/{Id}/stream.3gp", "GET")]
    [Route("/Videos/{Id}/stream.wmv", "GET")]
    [Route("/Videos/{Id}/stream.wtv", "GET")]
    [Route("/Videos/{Id}/stream", "GET")]
    [Route("/Videos/{Id}/stream.ts", "HEAD")]
    [Route("/Videos/{Id}/stream.webm", "HEAD")]
    [Route("/Videos/{Id}/stream.asf", "HEAD")]
    [Route("/Videos/{Id}/stream.wmv", "HEAD")]
    [Route("/Videos/{Id}/stream.ogv", "HEAD")]
    [Route("/Videos/{Id}/stream.mp4", "HEAD")]
    [Route("/Videos/{Id}/stream.m4v", "HEAD")]
    [Route("/Videos/{Id}/stream.mkv", "HEAD")]
    [Route("/Videos/{Id}/stream.mpeg", "HEAD")]
    [Route("/Videos/{Id}/stream.mpg", "HEAD")]
    [Route("/Videos/{Id}/stream.avi", "HEAD")]
    [Route("/Videos/{Id}/stream.3gp", "HEAD")]
    [Route("/Videos/{Id}/stream.wmv", "HEAD")]
    [Route("/Videos/{Id}/stream.wtv", "HEAD")]
    [Route("/Videos/{Id}/stream.m2ts", "HEAD")]
    [Route("/Videos/{Id}/stream", "HEAD")]
    [Api(Description = "Gets a video stream")]
    public class GetVideoStream : VideoStreamRequest
    {

    }

    /// <summary>
    /// Class VideoService
    /// </summary>
    public class VideoService : BaseProgressiveStreamingService
    {
        public VideoService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, IImageProcessor imageProcessor, IHttpClient httpClient) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder, imageProcessor, httpClient)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVideoStream request)
        {
            return ProcessRequest(request, false);
        }

        /// <summary>
        /// Heads the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Head(GetVideoStream request)
        {
            return ProcessRequest(request, true);
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            // Get the output codec name
            var videoCodec = state.OutputVideoCodec;

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (string.Equals(Path.GetExtension(outputPath), ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = GetNumberOfThreads(state, string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase));

            var inputModifier = GetInputModifier(state);

            return string.Format("{0} -i {1}{2} {3} {4} -map_metadata -1 -threads {5} {6}{7} -y \"{8}\"",
                inputModifier,
                GetInputArgument(state),
                keyFrame,
                GetMapArgs(state),
                GetVideoArguments(state, videoCodec),
                threads,
                GetAudioArguments(state),
                format,
                outputPath
                ).Trim();
        }

        /// <summary>
        /// Gets video arguments to pass to ffmpeg
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="codec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoArguments(StreamState state, string codec)
        {
            var args = "-vcodec " + codec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return state.VideoStream != null && IsH264(state.VideoStream) ? args + " -bsf h264_mp4toannexb" : args;
            }

            const string keyFrameArg = " -force_key_frames expr:if(isnan(prev_forced_t),gte(t,.1),gte(t,prev_forced_t+5))";

            args += keyFrameArg;

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            var request = state.VideoRequest;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
                {
                    args += GetOutputSizeParam(state, codec, CancellationToken.None);
                }
            }

            var qualityParam = GetVideoQualityParam(state, codec, false);

            if (!string.IsNullOrEmpty(qualityParam))
            {
                args += " " + qualityParam.Trim();
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetInternalGraphicalSubtitleParam(state, codec);
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
            if (state.AudioStream == null && state.VideoStream != null)
            {
                return string.Empty;
            }

            // Get the output codec name
            var codec = state.OutputAudioCodec;

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-acodec copy";
            }

            var args = "-acodec " + codec;

            // Add the number of audio channels
            var channels = state.OutputAudioChannels;

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(UsCulture);
            }

            args += " " + GetAudioFilterParam(state, true);

            return args;
        }
    }
}
