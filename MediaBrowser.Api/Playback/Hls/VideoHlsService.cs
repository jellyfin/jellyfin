using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using ServiceStack.ServiceHost;
using System;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class GetHlsVideoStream
    /// </summary>
    [Route("/Videos/{Id}/stream.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetHlsVideoStream : VideoStreamRequest
    {
        [ApiMember(Name = "BaselineStreamAudioBitRate", Description = "Optional. Specify the audio bitrate for the baseline stream.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? BaselineStreamAudioBitRate { get; set; }

        [ApiMember(Name = "AppendBaselineStream", Description = "Optional. Whether or not to include a baseline audio-only stream in the master playlist.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool AppendBaselineStream { get; set; }

        [ApiMember(Name = "TimeStampOffsetMs", Description = "Optional. Alter the timestamps in the playlist by a given amount, in ms. Default is 1000.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int TimeStampOffsetMs { get; set; }

        public GetHlsVideoStream()
        {
            TimeStampOffsetMs = 1000;
        }
    }

    /// <summary>
    /// Class VideoHlsService
    /// </summary>
    public class VideoHlsService : BaseHlsService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="dtoService">The dto service.</param>
        public VideoHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsVideoStream request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments(StreamState state)
        {
            var codec = GetAudioCodec(state.Request);

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }

            var args = "-codec:a:0 " + codec;

            if (state.AudioStream != null)
            {
                var channels = GetNumAudioChannelsParam(state.Request, state.AudioStream);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                if (state.Request.AudioSampleRate.HasValue)
                {
                    args += " -ar " + state.Request.AudioSampleRate.Value;
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

            return args;
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="performSubtitleConversion">if set to <c>true</c> [perform subtitle conversion].</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state, bool performSubtitleConversion)
        {
            var codec = GetVideoCodec(state.VideoRequest);

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf h264_mp4toannexb" : "-codec:v:0 copy";
            }

            const string keyFrameArg = " -force_key_frames expr:if(isnan(prev_forced_t),gte(t,.1),gte(t,prev_forced_t+5))";

            var args = "-codec:v:0 " + codec + " -preset superfast" + keyFrameArg;

            var bitrate = GetVideoBitrateParam(state);

            if (bitrate.HasValue)
            {
                args += string.Format(" -b:v {0} -maxrate ({0}*.85) -bufsize {0}", bitrate.Value.ToString(UsCulture));
            }
            
            // Add resolution params, if specified
            if (state.VideoRequest.Width.HasValue || state.VideoRequest.Height.HasValue || state.VideoRequest.MaxHeight.HasValue || state.VideoRequest.MaxWidth.HasValue)
            {
                args += GetOutputSizeParam(state, codec, performSubtitleConversion);
            }

            // Get the output framerate based on the FrameRate param
            var framerate = state.VideoRequest.Framerate ?? 0;

            // We have to supply a framerate for hls, so if it's null, account for that here
            if (state.VideoStream != null)
            {
                if (framerate.Equals(0))
                {
                    framerate = state.VideoStream.AverageFrameRate ?? 0;
                }
                if (framerate.Equals(0))
                {
                    framerate = state.VideoStream.RealFrameRate ?? 0;
                }
            }
            if (framerate.Equals(0))
            {
                framerate = 23.976;
            }

            framerate = Math.Round(framerate);

            args += string.Format(" -r {0}", framerate);

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
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return ".ts";
        }
    }
}
