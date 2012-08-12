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

        private string GetFFMpegOutputFormat(string outputFormat)
        {
            if (outputFormat.Equals("mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "matroska";
            }

            return outputFormat;
        }

        private int GetOutputAudioStreamIndex(string outputFormat)
        {
            return 0;
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            string outputFormat = GetOutputFormat();

            int audioStreamIndex = GetOutputAudioStreamIndex(outputFormat);

            List<string> maps = new List<string>();

            // Add the video stream
            maps.Add("-map 0:0");

            // Add the audio stream
            if (audioStreamIndex != -1)
            {
                maps.Add("-map 0:" + (1 + audioStreamIndex));
            }

            // Add all the subtitle streams
            for (int i = 0; i < LibraryItem.Subtitles.Count(); i++)
            {
                maps.Add("-map 0:" + (1 + LibraryItem.AudioStreams.Count() + i));

            }

            return string.Format("-i \"{0}\" {1} {2} {3} -f {4} -",
                LibraryItem.Path,
                string.Join(" ", maps.ToArray()),
                GetVideoArguments(),
                GetAudioArguments(),
                GetFFMpegOutputFormat(outputFormat)
                );
        }

        private string GetVideoArguments()
        {
            return "-c:v copy";
        }

        private string GetAudioArguments()
        {
            return "-c:a copy";
        }

        private string GetSubtitleArguments()
        {
            string args = "";

            for (int i = 0; i < LibraryItem.Subtitles.Count(); i++)
            {
                if (i > 0)
                {
                    args += " ";
                }
                args += "-c:s copy";

            }

            return args;
        }
    }
}
