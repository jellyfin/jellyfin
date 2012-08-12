using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class VideoHandler : BaseMediaHandler<Video>
    {
        private IEnumerable<string> UnsupportedOutputFormats = new string[] { "mp4" };

        public IEnumerable<string> VideoFormats
        {
            get
            {
                return QueryString["videoformats"].Split(',');
            }
        }

        /// <summary>
        /// Gets the format we'll be converting to
        /// </summary>
        protected override string GetOutputFormat()
        {
            return VideoFormats.First(f => !UnsupportedOutputFormats.Any(s => s.Equals(f, StringComparison.OrdinalIgnoreCase)));
        }

        protected override bool RequiresConversion()
        {
            // If it's not in a format we can output to, return true
            if (UnsupportedOutputFormats.Any(f => LibraryItem.Path.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // If it's not in a format the consumer accepts, return true
            if (!VideoFormats.Any(f => LibraryItem.Path.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
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

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            string outputFormat = GetOutputFormat();
            outputFormat = "matroska";
            return "-i \"" + LibraryItem.Path + "\"  -vcodec copy -acodec copy -f " + outputFormat + " -";
        }
    }
}
