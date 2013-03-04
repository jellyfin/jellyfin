using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using System;

namespace MediaBrowser.Api.Playback.Hls
{
    public class VideoHlsService : BaseHlsService
    {
        public VideoHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager)
            : base(appPaths, userManager, libraryManager, isoManager)
        {
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments(StreamState state)
        {
            if (!state.Request.AudioCodec.HasValue)
            {
                return "-codec:a:0 copy";
            }

            var codec = GetAudioCodec(state.Request);

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

                if (state.Request.AudioBitRate.HasValue)
                {
                    args += " -ab " + state.Request.AudioBitRate.Value;
                }

                return args;
            }

            return args;
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state)
        {
            var codec = GetVideoCodec(state.Request);

            // Right now all we support is either h264 or copy
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase) && !codec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                codec = "libx264";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var args = "-codec:v:0 " + codec + " -preset superfast";

            if (state.Request.VideoBitRate.HasValue)
            {
                args += string.Format(" -b:v {0}", state.Request.VideoBitRate.Value);
            }

            // Add resolution params, if specified
            if (state.Request.Width.HasValue || state.Request.Height.HasValue || state.Request.MaxHeight.HasValue || state.Request.MaxWidth.HasValue)
            {
                args += GetOutputSizeParam(state, codec);
            }

            // Get the output framerate based on the FrameRate param
            double framerate = state.Request.Framerate ?? 0;

            // We have to supply a framerate for hls, so if it's null, account for that here
            if (framerate.Equals(0))
            {
                framerate = state.VideoStream.AverageFrameRate ?? 0;
            }
            if (framerate.Equals(0))
            {
                framerate = state.VideoStream.RealFrameRate ?? 0;
            }
            if (framerate.Equals(0))
            {
                framerate = 23.976;
            }

            args += string.Format(" -r {0}", framerate);

            // Needed to ensure segments stay under 10 seconds
            args += string.Format(" -g {0}", framerate);

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
