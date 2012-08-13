using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Supported output formats are: mp3,flac,ogg,wav,asf,wma,aac
    /// </summary>
    public class AudioHandler : BaseMediaHandler<Audio>
    {
        /// <summary>
        /// Overriding to provide mp3 as a default, since pretty much every device supports it
        /// </summary>
        protected override IEnumerable<string> OutputFormats
        {
            get
            {
                IEnumerable<string> vals = base.OutputFormats;

                return vals.Any() ? vals : new string[] { "mp3" };
            }
        }

        /// <summary>
        /// We can output these files directly, but we can't encode them
        /// </summary>
        protected override IEnumerable<string> UnsupportedOutputEncodingFormats
        {
            get
            {
                return new string[] { "wma", "aac" };
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

            int index = OutputFormats.ToList().IndexOf(audioFormat);

            return AudioBitRates.ElementAt(index);
        }

        /// <summary>
        /// Determines whether or not the original file requires transcoding
        /// </summary>
        protected override bool RequiresConversion()
        {
            if (base.RequiresConversion())
            {
                return true;
            }

            string currentFormat = Path.GetExtension(LibraryItem.Path).Replace(".", string.Empty);
            
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
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            string outputFormat = GetConversionOutputFormat();

            int? bitrate = GetMaxAcceptedBitRate(outputFormat);

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value);
            }

            int? channels = GetNumAudioChannelsParam();

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + channels.Value);
            }

            int? sampleRate = GetSampleRateParam();

            if (sampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + sampleRate.Value);
            }

            audioTranscodeParams.Add("-f " + outputFormat);

            return "-i \"" + LibraryItem.Path + "\" -vn " + string.Join(" ", audioTranscodeParams.ToArray()) + " -";
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        private int? GetNumAudioChannelsParam()
        {
            // If the user requested a max number of channels
            if (AudioChannels.HasValue)
            {
                // Only specify the param if we're going to downmix
                if (AudioChannels.Value < LibraryItem.Channels)
                {
                    return AudioChannels.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        private int? GetSampleRateParam()
        {
            // If the user requested a max value
            if (AudioSampleRate.HasValue)
            {
                // Only specify the param if we're going to downmix
                if (AudioSampleRate.Value < LibraryItem.SampleRate)
                {
                    return AudioSampleRate.Value;
                }
            }

            return null;
        }
    }
}
