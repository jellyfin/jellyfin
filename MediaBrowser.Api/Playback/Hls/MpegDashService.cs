using System.Security;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Options is needed for chromecast. Threw Head in there since it's related
    /// </summary>
    [Route("/Videos/{Id}/master.mpd", "GET", Summary = "Gets a video stream using Mpeg dash.")]
    [Route("/Videos/{Id}/master.mpd", "HEAD", Summary = "Gets a video stream using Mpeg dash.")]
    public class GetMasterManifest : VideoStreamRequest
    {
        public bool EnableAdaptiveBitrateStreaming { get; set; }

        public GetMasterManifest()
        {
            EnableAdaptiveBitrateStreaming = true;
        }
    }

    [Route("/Videos/{Id}/dash/{SegmentId}.ts", "GET")]
    public class GetDashSegment : VideoStreamRequest
    {
        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    public class MpegDashService : BaseHlsService
    {
        protected INetworkManager NetworkManager { get; private set; }

        public MpegDashService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, INetworkManager networkManager)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
        {
            NetworkManager = networkManager;
        }

        public object Get(GetMasterManifest request)
        {
            var result = GetAsync(request, "GET").Result;

            return result;
        }

        public object Head(GetMasterManifest request)
        {
            var result = GetAsync(request, "HEAD").Result;

            return result;
        }

        private async Task<object> GetAsync(GetMasterManifest request, string method)
        {
            if (string.Equals(request.AudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Audio codec copy is not allowed here.");
            }

            if (string.Equals(request.VideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Video codec copy is not allowed here.");
            }

            if (string.IsNullOrEmpty(request.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required");
            }

            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var playlistText = string.Empty;

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                playlistText = GetManifestText(state);
            }

            return ResultFactory.GetResult(playlistText, Common.Net.MimeTypes.GetMimeType("playlist.mpd"), new Dictionary<string, string>());
        }

        private string GetManifestText(StreamState state)
        {
            var audioBitrate = state.OutputAudioBitrate ?? 0;
            var videoBitrate = state.OutputVideoBitrate ?? 0;

            var builder = new StringBuilder();

            var duration = "PT0H02M11.00S";

            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendFormat(
                "<MPD xmlns=\"urn:mpeg:dash:schema:mpd:2011\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:mpeg:dash:schema:mpd:2011 http://standards.iso.org/ittf/PubliclyAvailableStandards/MPEG-DASH_schema_files/DASH-MPD.xsd\" minBufferTime=\"PT2.00S\" mediaPresentationDuration=\"{0}\" maxSegmentDuration=\"PT{1}S\" type=\"static\" profiles=\"urn:mpeg:dash:profile:mp2t-simple:2011\">",
                duration,
                state.SegmentLength.ToString(CultureInfo.InvariantCulture));

            builder.Append("<ProgramInformation moreInformationURL=\"http://gpac.sourceforge.net\">");
            builder.Append("</ProgramInformation>");

            builder.AppendFormat("<Period start=\"PT0S\" duration=\"{0}\">", duration);
            builder.Append("<AdaptationSet segmentAlignment=\"true\">");

            builder.Append("<ContentComponent id=\"1\" contentType=\"video\"/>");

            var lang = state.AudioStream != null ? state.AudioStream.Language : null;
            if (string.IsNullOrWhiteSpace(lang)) lang = "und";

            builder.AppendFormat("<ContentComponent id=\"2\" contentType=\"audio\" lang=\"{0}\"/>", lang);

            builder.Append(GetRepresentationOpenElement(state, lang));

            AppendSegmentList(state, builder);

            builder.Append("</Representation>");
            builder.Append("</AdaptationSet>");
            builder.Append("</Period>");

            builder.Append("</MPD>");

            return builder.ToString();
        }

        private string GetRepresentationOpenElement(StreamState state, string language)
        {
            var codecs = GetVideoCodecDescriptor(state) + "," + GetAudioCodecDescriptor(state);

            var xml = "<Representation id=\"1\" mimeType=\"video/mp2t\" startWithSAP=\"1\" codecs=\"" + codecs + "\"";

            if (state.OutputWidth.HasValue)
            {
                xml += " width=\"" + state.OutputWidth.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputHeight.HasValue)
            {
                xml += " height=\"" + state.OutputHeight.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputAudioSampleRate.HasValue)
            {
                xml += " sampleRate=\"" + state.OutputAudioSampleRate.Value.ToString(UsCulture) + "\"";
            }

            if (state.TotalOutputBitrate.HasValue)
            {
                xml += " bandwidth=\"" + state.TotalOutputBitrate.Value.ToString(UsCulture) + "\"";
            }

            xml += ">";

            return xml;
        }

        private string GetVideoCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html

            var level = state.TargetVideoLevel ?? 0;
            var profile = state.TargetVideoProfile ?? string.Empty;

            if (profile.IndexOf("high", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4.1)
                {
                    return "avc1.640028";
                }

                if (level >= 4)
                {
                    return "avc1.640028";
                }

                return "avc1.64001f";
            }

            if (profile.IndexOf("main", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4)
                {
                    return "avc1.4d0028";
                }

                if (level >= 3.1)
                {
                    return "avc1.4d001f";
                }

                return "avc1.4d001e";
            }

            if (level >= 3.1)
            {
                return "avc1.42001f";
            }

            return "avc1.42001e";
        }

        private string GetAudioCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html

            if (string.Equals(state.OutputAudioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp4a.40.34";
            }

            // AAC 5ch
            if (state.OutputAudioChannels.HasValue && state.OutputAudioChannels.Value >= 5)
            {
                return "mp4a.40.5";
            }

            // AAC 2ch
            return "mp4a.40.2";
        }

        public object Get(GetDashSegment request)
        {
            return null;
        }

        private void AppendSegmentList(StreamState state, StringBuilder builder)
        {
            var seconds = TimeSpan.FromTicks(state.RunTimeTicks ?? 0).TotalSeconds;

            builder.Append("<SegmentList timescale=\"1000\" duration=\"10000\">");

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            var index = 0;

            while (seconds > 0)
            {
                builder.AppendFormat("<SegmentURL media=\"{0}.ts{1}\"/>", index.ToString(UsCulture), SecurityElement.Escape(queryString));

                seconds -= state.SegmentLength;
                index++;
            }
            builder.Append("</SegmentList>");
        }

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

        protected override string GetVideoArguments(StreamState state)
        {
            var codec = state.OutputVideoCodec;

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf:v h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                state.SegmentLength.ToString(UsCulture));

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            var args = "-codec:v:0 " + codec + " " + GetVideoQualityParam(state, "libx264", true) + keyFrameArg;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec, false);
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, codec);
            }

            return args;
        }

        protected override string GetCommandLineArguments(string outputPath, string transcodingJobId, StreamState state, bool isEncoding)
        {
            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var args = string.Format("{0} -i {1} -map_metadata -1 -threads {2} {3} {4} -copyts -flags -global_header {5} -hls_time {6} -start_number {7} -hls_list_size {8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(transcodingJobId, state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                state.SegmentLength.ToString(UsCulture),
                startNumberParam,
                state.HlsListSize.ToString(UsCulture),
                outputPath
                ).Trim();

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

        protected override TranscodingJobType TranscodingJobType
        {
            get
            {
                return TranscodingJobType.Dash;
            }
        }
    }
}
