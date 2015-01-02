using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.IO;

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
    }

    [Route("/Videos/{Id}/live.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetLiveHlsStream : VideoStreamRequest
    {
    }

    /// <summary>
    /// Class GetHlsVideoSegment
    /// </summary>
    [Route("/Videos/{Id}/hls/{PlaylistId}/{SegmentId}.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsVideoSegment : VideoStreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    /// <summary>
    /// Class VideoHlsService
    /// </summary>
    public class VideoHlsService : BaseHlsService
    {
        public VideoHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsVideoSegment request)
        {
            var file = request.SegmentId + Path.GetExtension(Request.PathInfo);

            file = Path.Combine(ServerConfigurationManager.ApplicationPaths.TranscodingTempPath, EncodingContext.Streaming.ToString().ToLower(), file);

            return ResultFactory.GetStaticFileResult(Request, file);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsVideoStream request)
        {
            return ProcessRequest(request, false);
        }

        public object Get(GetLiveHlsStream request)
        {
            return ProcessRequest(request, true);
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments(StreamState state)
        {
            var codec = state.OutputAudioCodec;

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }

            var args = "-codec:a:0 " + codec;

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

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state)
        {
            var codec = state.OutputVideoCodec;

            var args = "-codec:v:0 " + codec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return state.VideoStream != null && IsH264(state.VideoStream) ?
                    args + " -bsf:v h264_mp4toannexb" :
                    args;
            }
            
            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                state.SegmentLength.ToString(UsCulture));

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            args += " " + GetVideoQualityParam(state, H264Encoder, true) + keyFrameArg;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec);
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, codec);
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
