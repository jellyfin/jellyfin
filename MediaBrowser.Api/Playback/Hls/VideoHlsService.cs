using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    [Route("/Videos/{Id}/master.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetMasterHlsVideoStream : VideoStreamRequest
    {
        [ApiMember(Name = "BaselineStreamAudioBitRate", Description = "Optional. Specify the audio bitrate for the baseline stream.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? BaselineStreamAudioBitRate { get; set; }

        [ApiMember(Name = "AppendBaselineStream", Description = "Optional. Whether or not to include a baseline audio-only stream in the master playlist.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool AppendBaselineStream { get; set; }
    }

    [Route("/Videos/{Id}/main.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetMainHlsVideoStream : VideoStreamRequest
    {
    }

    [Route("/Videos/{Id}/baseline.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetBaselineHlsVideoStream : VideoStreamRequest
    {
    }

    /// <summary>
    /// Class VideoHlsService
    /// </summary>
    public class VideoHlsService : BaseHlsService
    {
        public VideoHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService, IFileSystem fileSystem, IItemRepository itemRepository, ILiveTvManager liveTvManager)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, dtoService, fileSystem, itemRepository, liveTvManager)
        {
        }

        public object Get(GetMasterHlsVideoStream request)
        {
            var result = GetAsync(request).Result;

            return result;
        }

        public object Get(GetMainHlsVideoStream request)
        {
            var result = GetPlaylistAsync(request, "main").Result;

            return result;
        }

        public object Get(GetBaselineHlsVideoStream request)
        {
            var result = GetPlaylistAsync(request, "baseline").Result;

            return result;
        }

        private async Task<object> GetPlaylistAsync(VideoStreamRequest request, string name)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");
            builder.AppendLine("#EXT-X-VERSION:3");
            builder.AppendLine("#EXT-X-TARGETDURATION:" + state.SegmentLength.ToString(UsCulture));
            builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);
            
            var seconds = TimeSpan.FromTicks(state.RunTimeTicks ?? 0).TotalSeconds;

            var index = 0;

            while (seconds > 0)
            {
                var length = seconds >= state.SegmentLength ? state.SegmentLength : seconds;

                builder.AppendLine("#EXTINF:" + length.ToString(UsCulture));

                builder.AppendLine(string.Format("hls/{0}/{1}.ts{2}" ,

                    name,
                    index.ToString(UsCulture),
                    queryString));

                seconds -= state.SegmentLength;
                index++;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            var playlistText = builder.ToString();

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private async Task<object> GetAsync(GetMasterHlsVideoStream request)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            if (!state.VideoRequest.VideoBitRate.HasValue && (!state.VideoRequest.VideoCodec.HasValue || state.VideoRequest.VideoCodec.Value != VideoCodecs.Copy))
            {
                throw new ArgumentException("A video bitrate is required");
            }
            if (!state.Request.AudioBitRate.HasValue && (!state.Request.AudioCodec.HasValue || state.Request.AudioCodec.Value != AudioCodecs.Copy))
            {
                throw new ArgumentException("An audio bitrate is required");
            }

            int audioBitrate;
            int videoBitrate;
            GetPlaylistBitrates(state, out audioBitrate, out videoBitrate);

            var appendBaselineStream = false;
            var baselineStreamBitrate = 64000;

            var hlsVideoRequest = state.VideoRequest as GetMasterHlsVideoStream;
            if (hlsVideoRequest != null)
            {
                appendBaselineStream = hlsVideoRequest.AppendBaselineStream;
                baselineStreamBitrate = hlsVideoRequest.BaselineStreamAudioBitRate ?? baselineStreamBitrate;
            }

            var playlistText = GetMasterPlaylistFileText(videoBitrate + audioBitrate, appendBaselineStream, baselineStreamBitrate);

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetMasterPlaylistFileText(int bitrate, bool includeBaselineStream, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.05);

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            // Main stream
            builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + paddedBitrate.ToString(UsCulture));
            var playlistUrl = "main.m3u8" + queryString;
            builder.AppendLine(playlistUrl);

            // Low bitrate stream
            if (includeBaselineStream)
            {
                builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + baselineStreamBitrate.ToString(UsCulture));
                playlistUrl = "baseline.m3u8" + queryString;
                builder.AppendLine(playlistUrl);
            }

            return builder.ToString();
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

                var bitrate = GetAudioBitrateParam(state);

                if (bitrate.HasValue)
                {
                    args += " -ab " + bitrate.Value.ToString(UsCulture);
                }

                args += " " + GetAudioFilterParam(state, true);

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

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsExternal &&
                                 (state.SubtitleStream.Codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) != -1 ||
                                  state.SubtitleStream.Codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1);

            var args = "-codec:v:0 " + codec + " " + GetVideoQualityParam(state, "libx264") + keyFrameArg;

            var bitrate = GetVideoBitrateParam(state);

            if (bitrate.HasValue)
            {
                args += string.Format(" -b:v {0} -maxrate ({0}*.80) -bufsize {0}", bitrate.Value.ToString(UsCulture));
            }

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                if (state.VideoRequest.Width.HasValue || state.VideoRequest.Height.HasValue || state.VideoRequest.MaxHeight.HasValue || state.VideoRequest.MaxWidth.HasValue)
                {
                    args += GetOutputSizeParam(state, codec, performSubtitleConversion);
                }
            }

            if (state.VideoRequest.Framerate.HasValue)
            {
                args += string.Format(" -r {0}", state.VideoRequest.Framerate.Value);
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

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetInternalGraphicalSubtitleParam(state, codec);
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
