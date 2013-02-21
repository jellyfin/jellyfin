using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using System;
using System.ComponentModel.Composition;
using System.Net;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Class HlsVideoPlaylistHandler
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    public class HlsVideoPlaylistHandler : BaseHlsPlaylistHandler<Video>
    {
        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("video.m3u8", request);
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <value>The segment file extension.</value>
        protected override string SegmentFileExtension
        {
            get { return ".ts"; }
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments()
        {
            var codec = GetVideoCodec();

            // Right now all we support is either h264 or copy
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase) && !codec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                codec = "libx264";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(VideoStream) ? "-codec:v:0 copy -bsf h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var args = "-codec:v:0 " + codec + " -preset superfast";

            if (VideoBitRate.HasValue)
            {
                args += string.Format(" -b:v {0}", VideoBitRate.Value);
            }

            // Add resolution params, if specified
            if (Width.HasValue || Height.HasValue || MaxHeight.HasValue || MaxWidth.HasValue)
            {
                args += GetOutputSizeParam(codec);
            }

            // Get the output framerate based on the FrameRate param
            double framerate = FrameRate ?? 0;

            // We have to supply a framerate for hls, so if it's null, account for that here
            if (framerate.Equals(0))
            {
                framerate = VideoStream.AverageFrameRate ?? 0;
            }
            if (framerate.Equals(0))
            {
                framerate = VideoStream.RealFrameRate ?? 0;
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
        /// Gets the audio arguments to pass to ffmpeg
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments()
        {
            if (!AudioCodec.HasValue)
            {
                return "-codec:a:0 copy";
            }

            var codec = GetAudioCodec();

            var args = "-codec:a:0 " + codec;

            if (AudioStream != null)
            {
                var channels = GetNumAudioChannelsParam();

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                var sampleRate = GetSampleRateParam();

                if (sampleRate.HasValue)
                {
                    args += " -ar " + sampleRate.Value;
                }

                if (AudioBitRate.HasValue)
                {
                    args += " -ab " + AudioBitRate.Value;
                }

                return args;
            }

            return args;
        }
    }
}
