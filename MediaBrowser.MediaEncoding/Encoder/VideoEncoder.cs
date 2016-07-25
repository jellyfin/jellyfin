using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class VideoEncoder : BaseEncoder
    {
        public VideoEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IIsoManager isoManager, ILibraryManager libraryManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder, IMediaSourceManager mediaSourceManager) : base(mediaEncoder, logger, configurationManager, fileSystem, isoManager, libraryManager, sessionManager, subtitleEncoder, mediaSourceManager)
        {
        }

        protected override async Task<string> GetCommandLineArguments(EncodingJob state)
        {
            // Get the output codec name
            var videoCodec = EncodingJobFactory.GetVideoEncoder(state, GetEncodingOptions());

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (string.Equals(Path.GetExtension(state.OutputFilePath), ".mp4", StringComparison.OrdinalIgnoreCase) &&
                state.Options.Context == EncodingContext.Streaming)
            {
                // Comparison: https://github.com/jansmolders86/mediacenterjs/blob/master/lib/transcoding/desktop.js
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = GetNumberOfThreads(state, string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase));

            var inputModifier = GetInputModifier(state);

            var videoArguments = await GetVideoArguments(state, videoCodec).ConfigureAwait(false);

            return string.Format("{0} {1}{2} {3} {4} -map_metadata -1 -threads {5} {6}{7} -y \"{8}\"",
                inputModifier,
                GetInputArgument(state),
                keyFrame,
                GetMapArgs(state),
                videoArguments,
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
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        private async Task<string> GetVideoArguments(EncodingJob state, string videoCodec)
        {
            var args = "-codec:v:0 " + videoCodec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            var isOutputMkv = string.Equals(state.Options.OutputContainer, "mkv", StringComparison.OrdinalIgnoreCase);

            if (state.RunTimeTicks.HasValue)
            {
                //args += " -copyts -avoid_negative_ts disabled -start_at_zero";
            }

            if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                if (state.VideoStream != null && IsH264(state.VideoStream) && string.Equals(state.Options.OutputContainer, "ts", StringComparison.OrdinalIgnoreCase) && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -bsf:v h264_mp4toannexb";
                }

                return args;
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                5.ToString(UsCulture));

            args += keyFrameArg;

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.Options.SubtitleMethod == SubtitleDeliveryMethod.Encode;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += await GetOutputSizeParam(state, videoCodec).ConfigureAwait(false);
            }

            var qualityParam = GetVideoQualityParam(state, videoCodec);

            if (!string.IsNullOrEmpty(qualityParam))
            {
                args += " " + qualityParam.Trim();
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += await GetGraphicalSubtitleParam(state, videoCodec).ConfigureAwait(false);
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
            var codec = EncodingJobFactory.GetAudioEncoder(state);

            var args = "-codec:a:0 " + codec;

            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
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
