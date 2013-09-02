using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using ServiceStack.ServiceHost;
using System;
using System.IO;

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
    [Route("/Videos/{Id}/stream.m2ts", "GET")]
    [Route("/Videos/{Id}/stream.3gp", "GET")]
    [Route("/Videos/{Id}/stream.wmv", "GET")]
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
    [Route("/Videos/{Id}/stream.avi", "HEAD")]
    [Route("/Videos/{Id}/stream.3gp", "HEAD")]
    [Route("/Videos/{Id}/stream.wmv", "HEAD")]
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
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoService"/> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        public VideoService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepo)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, itemRepo)
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
        /// <param name="performSubtitleConversions">if set to <c>true</c> [perform subtitle conversions].</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool performSubtitleConversions)
        {
            var video = (Video)state.Item;

            var probeSize = GetProbeSizeArgument(state.Item);

            // Get the output codec name
            var videoCodec = GetVideoCodec(state.VideoRequest);

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (string.Equals(Path.GetExtension(outputPath), ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            return string.Format("{0} {1} {2} -i {3}{4}{5} {6} {7} -threads {8} {9}{10} \"{11}\"",
                probeSize,
                GetUserAgentParam(state.Item),
                GetFastSeekCommandLineParameter(state.Request),
                GetInputArgument(video, state.IsoMount),
                GetSlowSeekCommandLineParameter(state.Request),
                keyFrame,
                GetMapArgs(state),
                GetVideoArguments(state, videoCodec, performSubtitleConversions),
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
        /// <param name="performSubtitleConversion">if set to <c>true</c> [perform subtitle conversion].</param>
        /// <returns>System.String.</returns>
        private string GetVideoArguments(StreamState state, string codec, bool performSubtitleConversion)
        {
            var args = "-vcodec " + codec;

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return state.VideoStream != null && IsH264(state.VideoStream) ? args + " -bsf h264_mp4toannexb" : args;
            }

            const string keyFrameArg = " -force_key_frames expr:if(isnan(prev_forced_t),gte(t,.1),gte(t,prev_forced_t+5))";

            args += keyFrameArg;

            var request = state.VideoRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                args += GetOutputSizeParam(state, codec, performSubtitleConversion);
            }

            if (request.Framerate.HasValue)
            {
                args += string.Format(" -r {0}", request.Framerate.Value);
            }

            var qualityParam = GetVideoQualityParam(state, codec);

            if (!string.IsNullOrEmpty(qualityParam))
            {
                args += " " + qualityParam;
            }

            args += " -vsync vfr";

            if (!string.IsNullOrEmpty(state.VideoRequest.Profile))
            {
                args += " -profile:v " + state.VideoRequest.Profile;
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Level))
            {
                args += " -level " + state.VideoRequest.Level;
            }

            if (state.SubtitleStream != null)
            {
                // This is for internal graphical subs
                if (!state.SubtitleStream.IsExternal && (state.SubtitleStream.Codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) != -1 || state.SubtitleStream.Codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    args += GetInternalGraphicalSubtitleParam(state, codec);
                }
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

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-acodec copy";
            }
            
            var args = "-acodec " + codec;
            
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

            var bitrate = GetAudioBitrateParam(state);

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(UsCulture);
            }

            var volParam = string.Empty;

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2 && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5)
            {
                volParam = ",volume=2.000000";
            }

            args += string.Format(" -af \"aresample=async=1000{0}\"", volParam);

            return args;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoQualityParam(StreamState state, string videoCodec)
        {
            var args = string.Empty;

            // webm
            if (videoCodec.Equals("libvpx", StringComparison.OrdinalIgnoreCase))
            {
                args = "-speed 16 -quality good -profile:v 0 -slices 8";
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
            else if (videoCodec.Equals("mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                args = "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }

            var bitrate = GetVideoBitrateParam(state);

            if (bitrate.HasValue)
            {
                args += string.Format(" -b:v {0} -maxrate {0} -minrate {0} -bufsize ({0}/11)", bitrate.Value.ToString(UsCulture));
            }

            return args.Trim();
        }
    }
}
