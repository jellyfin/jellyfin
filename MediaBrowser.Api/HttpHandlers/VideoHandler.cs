using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using System.IO;

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
            string codec = GetAudioCodec(outputFormat);

            string args = "-acodec " + codec;

            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                int? channels = GetNumAudioChannels(codec);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }
                if (AudioSampleRate.HasValue)
                {
                    args += " -ar " + AudioSampleRate.Value;
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
            else if (outputFormat.Equals("flv"))
            {
                return "libx264";
            }
            else if (outputFormat.Equals("ts"))
            {
                return "libx264";
            }
            else if (outputFormat.Equals("asf"))
            {
                return "wmv2";
            }

            return "copy";
        }
        
        private string GetAudioCodec(string outputFormat)
        {
            if (outputFormat.Equals("webm"))
            {
                // Per webm specification, it must be vorbis
                return "libvorbis";
            }
            else if (outputFormat.Equals("flv"))
            {
                return "libvo_aacenc";
            }
            else if (outputFormat.Equals("ts"))
            {
                return "libvo_aacenc";
            }
            else if (outputFormat.Equals("asf"))
            {
                return "libvo_aacenc";
            }

            return "copy";
        }

        private int? GetNumAudioChannels(string audioCodec)
        {
            if (audioCodec.Equals("libvo_aacenc"))
            {
                // libvo_aacenc currently only supports two channel output
                return 2;
            }
            
            return AudioChannels;
        }
    }
}
