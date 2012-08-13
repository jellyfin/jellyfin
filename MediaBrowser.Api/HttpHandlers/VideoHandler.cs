using System;
using System.Collections.Generic;
using System.IO;
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
                return new string[] { "mp4", "wmv", "3gp", "avi", "ogv", "mov", "m4v", "mkv" };
            }
        }

        protected override bool RequiresConversion()
        {
            string currentFormat = Path.GetExtension(LibraryItem.Path).Replace(".", string.Empty);

            // For now we won't allow these to pass through.
            // Later we'll add some intelligence to allow it when possible
            if (currentFormat.Equals("mp4", StringComparison.OrdinalIgnoreCase) || currentFormat.Equals("mkv", StringComparison.OrdinalIgnoreCase) || currentFormat.Equals("m4v", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

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

        /// <summary>
        /// Translates the file extension to the format param that follows "-f" on the ffmpeg command line
        /// </summary>
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

            return string.Format("-i \"{0}\" -threads 0 {1} {2} -f {3} -",
                LibraryItem.Path,
                GetVideoArguments(outputFormat),
                GetAudioArguments(outputFormat),
                GetFFMpegOutputFormat(outputFormat)
                );
        }

        private string GetVideoArguments(string outputFormat)
        {
            string codec = GetVideoCodec(outputFormat);

            string args = "-vcodec " + codec;

            return args;
        }

        private string GetAudioArguments(string outputFormat)
        {
            AudioStream audioStream = LibraryItem.AudioStreams.FirstOrDefault();

            if (audioStream == null)
            {
                return string.Empty;
            }

            string codec = GetAudioCodec(audioStream, outputFormat);

            string args = "-acodec " + codec;

            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                int? channels = GetNumAudioChannelsParam(codec, audioStream.Channels);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                int? sampleRate = GetSampleRateParam(audioStream.SampleRate);

                if (sampleRate.HasValue)
                {
                    args += " -ar " + sampleRate.Value;
                }

            }

            return args;
        }

        private string GetVideoCodec(string outputFormat)
        {
            if (outputFormat.Equals("webm"))
            {
                // Per webm specification, it must be vpx
                return "libvpx";
            }
            else if (outputFormat.Equals("asf"))
            {
                return "wmv2";
            }

            return "libx264";
        }

        private string GetAudioCodec(AudioStream audioStream, string outputFormat)
        {
            if (outputFormat.Equals("webm"))
            {
                // Per webm specification, it must be vorbis
                return "libvorbis";
            }

            // See if we can just copy the stream
            if (HasBasicAudio(audioStream))
            {
                return "copy";
            }

            return "libvo_aacenc";
        }

        private int? GetNumAudioChannelsParam(string audioCodec, int libraryItemChannels)
        {
            if (libraryItemChannels > 2 && audioCodec.Equals("libvo_aacenc"))
            {
                // libvo_aacenc currently only supports two channel output
                return 2;
            }

            return GetNumAudioChannelsParam(libraryItemChannels);
        }

        private bool HasBasicAudio(AudioStream audio)
        {
            int maxChannels = AudioChannels ?? 2;

            if (audio.Channels > maxChannels)
            {
                return false;
            }

            if (audio.AudioFormat.IndexOf("aac", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return true;
            }
            if (audio.AudioFormat.IndexOf("ac-3", StringComparison.OrdinalIgnoreCase) != -1 || audio.AudioFormat.IndexOf("ac3", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return true;
            }
            if (audio.AudioFormat.IndexOf("mpeg", StringComparison.OrdinalIgnoreCase) != -1 || audio.AudioFormat.IndexOf("mp3", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return true;
            }

            return false;
        }
    }
}
