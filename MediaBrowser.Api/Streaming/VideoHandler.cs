using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Providers a progressive streaming video api
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    class VideoHandler : BaseProgressiveStreamingHandler<Video>
    {
        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return EntityResolutionHelper.VideoFileExtensions.Any(a => ApiService.IsApiUrlMatch("video" + a, request));
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, IIsoMount isoMount)
        {
            var probeSize = Kernel.FFMpegManager.GetProbeSizeArgument(LibraryItem.VideoType, LibraryItem.IsoType);

            // Get the output codec name
            var videoCodec = GetVideoCodec();

            var graphicalSubtitleParam = string.Empty;

            if (SubtitleStream != null)
            {
                // This is for internal graphical subs
                if (!SubtitleStream.IsExternal && (SubtitleStream.Codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) != -1 || SubtitleStream.Codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    graphicalSubtitleParam = GetInternalGraphicalSubtitleParam(SubtitleStream, videoCodec);
                }
            }

            return string.Format("{0} {1} -i {2}{3} -threads 0 {4} {5}{6} {7} \"{8}\"",
                probeSize,
                FastSeekCommandLineParameter,
                GetInputArgument(isoMount),
                SlowSeekCommandLineParameter,
                MapArgs,
                GetVideoArguments(videoCodec),
                graphicalSubtitleParam,
                GetAudioArguments(),
                outputPath
                ).Trim();
        }

        /// <summary>
        /// Gets video arguments to pass to ffmpeg
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetVideoArguments(string videoCodec)
        {
            var args = "-vcodec " + videoCodec;

            // If we're encoding video, add additional params
            if (!videoCodec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add resolution params, if specified
                if (Width.HasValue || Height.HasValue || MaxHeight.HasValue || MaxWidth.HasValue)
                {
                    args += GetOutputSizeParam(videoCodec);
                }

                if (FrameRate.HasValue)
                {
                    args += string.Format(" -r {0}", FrameRate.Value);
                }

                // Add the audio bitrate
                var qualityParam = GetVideoQualityParam(videoCodec);

                if (!string.IsNullOrEmpty(qualityParam))
                {
                    args += " " + qualityParam;
                }
            }
            else if (IsH264(VideoStream))
            {
                args += " -bsf h264_mp4toannexb";
            }

            return args;
        }

        /// <summary>
        /// Gets audio arguments to pass to ffmpeg
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetAudioArguments()
        {
            // If the video doesn't have an audio stream, return a default.
            if (AudioStream == null)
            {
                return string.Empty;
            }

            // Get the output codec name
            var codec = GetAudioCodec();

            var args = "-acodec " + codec;

            // If we're encoding audio, add additional params
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add the number of audio channels
                var channels = GetNumAudioChannelsParam();

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                // Add the audio sample rate
                var sampleRate = GetSampleRateParam();

                if (sampleRate.HasValue)
                {
                    args += " -ar " + sampleRate.Value;
                }

                if (AudioBitRate.HasValue)
                {
                    args += " -ab " + AudioBitRate.Value;
                }
            }

            return args;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        private string GetVideoQualityParam(string videoCodec)
        {
            var args = string.Empty;

            // webm
            if (videoCodec.Equals("libvpx", StringComparison.OrdinalIgnoreCase))
            {
                args = "-g 120 -cpu-used 1 -lag-in-frames 16 -deadline realtime -slices 4 -vprofile 0";
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

            if (VideoBitRate.HasValue)
            {
                args += " -b:v " + VideoBitRate;
            }

            return args.Trim();
        }
    }
}
