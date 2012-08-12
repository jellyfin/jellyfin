using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Supported output formats: mkv,m4v,mp4,asf,wmv,mov,webm,ogv,3gp,avi,ts,flv
    /// </summary>
    class VideoHandler : BaseMediaHandler<Video>
    {
        /// <summary>
        /// We can output these files directly, but we can't encode them
        /// </summary>
        protected override IEnumerable<string> UnsupportedOutputEncodingFormats
        {
            get
            {
                return new string[] { "mp4", "wmv" };
            }
        }

        protected override bool RequiresConversion()
        {
            if (base.RequiresConversion())
            {
                return true;
            }

            AudioStream audio = LibraryItem.AudioStreams.FirstOrDefault();

            if (audio != null)
            {
                // If the number of channels is greater than our desired channels, we need to transcode
                if (AudioChannels.HasValue && AudioChannels.Value < audio.Channels)
                {
                    return true;
                }
            }

            // Yay
            return false;
        }

        private string GetFFMpegOutputFormat(string outputFormat)
        {
            if (outputFormat.Equals("mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "matroska";
            }
            else if (outputFormat.Equals("ts", StringComparison.OrdinalIgnoreCase))
            {
                return "mpegts";
            }
            else if (outputFormat.Equals("ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "ogg";
            }

            return outputFormat;
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            string outputFormat = GetConversionOutputFormat();

            return string.Format("-i \"{0}\" {1} {2} -f {3} -",
                LibraryItem.Path,
                GetVideoArguments(),
                GetAudioArguments(),
                GetFFMpegOutputFormat(outputFormat)
                );
        }

        private string GetVideoArguments()
        {
            return "-vcodec copy";
        }

        private string GetAudioArguments()
        {
            return "-acodec copy";
        }
    }
}
