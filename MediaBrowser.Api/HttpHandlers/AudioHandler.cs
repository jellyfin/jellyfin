using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Supported output formats are: mp3,flac,ogg,wav,asf,wma,aac
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class AudioHandler : BaseMediaHandler<Audio, AudioOutputFormats>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("audio", request);
        }

        /// <summary>
        /// We can output these formats directly, but we cannot encode to them.
        /// </summary>
        protected override IEnumerable<AudioOutputFormats> UnsupportedOutputEncodingFormats
        {
            get
            {
                return new AudioOutputFormats[] { AudioOutputFormats.Aac, AudioOutputFormats.Flac, AudioOutputFormats.Wma };
            }
        }

        private int? GetMaxAcceptedBitRate(AudioOutputFormats audioFormat)
        {
            return GetMaxAcceptedBitRate(audioFormat.ToString());
        }

        private int? GetMaxAcceptedBitRate(string audioFormat)
        {
            if (audioFormat.Equals("mp3", System.StringComparison.OrdinalIgnoreCase))
            {
                return 320000;
            }

            return null;
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
            var audioTranscodeParams = new List<string>();

            AudioOutputFormats outputFormat = GetConversionOutputFormat();

            int? bitrate = GetMaxAcceptedBitRate(outputFormat);

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value);
            }

            int? channels = GetNumAudioChannelsParam(LibraryItem.Channels);

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + channels.Value);
            }

            int? sampleRate = GetSampleRateParam(LibraryItem.SampleRate);

            if (sampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + sampleRate.Value);
            }

            audioTranscodeParams.Add("-f " + outputFormat);

            return "-i \"" + LibraryItem.Path + "\" -vn " + string.Join(" ", audioTranscodeParams.ToArray()) + " -";
        }
    }
}
