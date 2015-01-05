using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class VideoEncoder : BaseEncoder
    {
        public VideoEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, ILiveTvManager liveTvManager, IIsoManager isoManager, ILibraryManager libraryManager, IChannelManager channelManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder) : base(mediaEncoder, logger, configurationManager, fileSystem, liveTvManager, isoManager, libraryManager, channelManager, sessionManager, subtitleEncoder)
        {
        }

        protected override string GetCommandLineArguments(EncodingJob state)
        {
            // Get the output codec name
            var videoCodec = state.OutputVideoCodec;

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (string.Equals(Path.GetExtension(state.OutputFilePath), ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = GetNumberOfThreads(state, string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase));

            var inputModifier = GetInputModifier(state);

            return string.Format("{0} {1}{2} {3} {4} -map_metadata -1 -threads {5} {6}{7} -y \"{8}\"",
                inputModifier,
                GetInputArgument(state),
                keyFrame,
                GetMapArgs(state),
                GetVideoArguments(state, videoCodec),
                threads,
                GetAudioArguments(state),
                format,
                state.OutputFilePath
                ).Trim();
        }

        /// <summary>
        /// Gets video arguments to pass to ffmpeg
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="codec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoArguments(EncodingJob state, string codec)
        {
            var args = "-codec:v:0 " + codec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return state.VideoStream != null && IsH264(state.VideoStream) && string.Equals(state.Options.OutputContainer, "ts", StringComparison.OrdinalIgnoreCase) ?
                    args + " -bsf:v h264_mp4toannexb" :
                    args;
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                5.ToString(UsCulture));

            args += keyFrameArg;

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec);
            }

            var qualityParam = GetVideoQualityParam(state, codec, false);

            if (!string.IsNullOrEmpty(qualityParam))
            {
                args += " " + qualityParam.Trim();
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, codec);
            }

            return args;
        }

        /// <summary>
        /// Gets audio arguments to pass to ffmpeg
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetAudioArguments(EncodingJob state)
        {
            // If the video doesn't have an audio stream, return a default.
            if (state.AudioStream == null && state.VideoStream != null)
            {
                return string.Empty;
            }

            // Get the output codec name
            var codec = state.OutputAudioCodec;

            var args = "-codec:a:0 " + codec;

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return args;
            }

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

            args += " " + GetAudioFilterParam(state, false);

            return args;
        }

        protected override string GetOutputFileExtension(EncodingJob state)
        {
            var ext = base.GetOutputFileExtension(state);

            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }

            var videoCodec = state.Options.VideoCodec;

            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                return ".ts";
            }
            if (string.Equals(videoCodec, "theora", StringComparison.OrdinalIgnoreCase))
            {
                return ".ogv";
            }
            if (string.Equals(videoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
            {
                return ".webm";
            }
            if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
            {
                return ".asf";
            }

            return null;
        }

        protected override bool IsVideoEncoder
        {
            get { return true; }
        }
    }
}
