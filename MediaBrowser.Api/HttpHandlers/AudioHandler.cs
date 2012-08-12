using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class AudioHandler : BaseMediaHandler<Audio>
    {
        /// <summary>
        /// Supported values: mp3,flac,ogg,wav,asf
        /// </summary>
        public IEnumerable<string> AudioFormats
        {
            get
            {
                string val = QueryString["audioformats"];

                if (string.IsNullOrEmpty(val))
                {
                    return new string[] { "mp3" };
                }

                return val.Split(',');
            }
        }

        public IEnumerable<int> AudioBitRates
        {
            get
            {
                string val = QueryString["audiobitrates"];

                if (string.IsNullOrEmpty(val))
                {
                    return new int[] { };
                }

                return val.Split(',').Select(v => int.Parse(v));
            }
        }

        private int? GetMaxAcceptedBitRate(string audioFormat)
        {
            if (!AudioBitRates.Any())
            {
                return null;
            }

            int index = AudioFormats.ToList().IndexOf(audioFormat);

            return AudioBitRates.ElementAt(index);
        }

        /// <summary>
        /// Determines whether or not the original file requires transcoding
        /// </summary>
        protected override bool RequiresConversion()
        {
            string currentFormat = Path.GetExtension(LibraryItem.Path).Replace(".", string.Empty);

            // If it's not in a format the consumer accepts, return true
            if (!AudioFormats.Any(f => currentFormat.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            int? bitrate = GetMaxAcceptedBitRate(currentFormat);

            // If the bitrate is greater than our desired bitrate, we need to transcode
            if (bitrate.HasValue && bitrate.Value < LibraryItem.BitRate)
            {
                return true;
            }

            // If the number of channels is greater than our desired channels, we need to transcode
            if (AudioChannels.HasValue && AudioChannels.Value < LibraryItem.Channels)
            {
                return true;
            }

            // If the sample rate is greater than our desired sample rate, we need to transcode
            if (AudioSampleRate.HasValue && AudioSampleRate.Value < LibraryItem.SampleRate)
            {
                return true;
            }

            // Yay
            return false;
        }

        /// <summary>
        /// Gets the format we'll be converting to
        /// </summary>
        protected override string GetOutputFormat()
        {
            return AudioFormats.First();
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            string outputFormat = GetOutputFormat();

            int? bitrate = GetMaxAcceptedBitRate(outputFormat);

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value);
            }

            if (AudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + AudioChannels.Value);
            }

            if (AudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + AudioSampleRate.Value);
            }

            audioTranscodeParams.Add("-f " + outputFormat);

            return "-i \"" + LibraryItem.Path + "\" -vn " + string.Join(" ", audioTranscodeParams.ToArray()) + " -";
        }
    }
}
