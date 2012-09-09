using MediaBrowser.Common.Drawing;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Supported output formats: mkv,m4v,mp4,asf,wmv,mov,webm,ogv,3gp,avi,ts,flv
    /// </summary>
    [Export(typeof(BaseHandler))]
    class VideoHandler : BaseMediaHandler<Video, string>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("video", request);
        }

        /// <summary>
        /// We can output these files directly, but we can't encode them
        /// </summary>
        protected override IEnumerable<string> UnsupportedOutputEncodingFormats
        {
            get
            {
                // mp4, 3gp, mov - muxer does not support non-seekable output
                // avi, mov, mkv, m4v - can't stream these when encoding. the player will try to download them completely before starting playback.
                // wmv - can't seem to figure out the output format name
                return new string[] { "mp4", "3gp", "m4v", "mkv", "avi", "mov", "wmv" };
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

            if (RequiresVideoConversion())
            {
                return true;
            }

            AudioStream audioStream = (LibraryItem.AudioStreams ?? new List<AudioStream>()).FirstOrDefault();

            if (audioStream != null)
            {
                if (RequiresAudioConversion(audioStream))
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

            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                if (Width.HasValue || Height.HasValue || MaxHeight.HasValue || MaxWidth.HasValue)
                {
                    Size size = DrawingUtils.Resize(LibraryItem.Width, LibraryItem.Height, Width, Height, MaxWidth, MaxHeight);

                    args += string.Format(" -s {0}x{1}", size.Width, size.Height);
                }
            }

            return args;
        }

        private string GetAudioArguments(string outputFormat)
        {
            AudioStream audioStream = (LibraryItem.AudioStreams ?? new List<AudioStream>()).FirstOrDefault();

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
            else if (outputFormat.Equals("wmv"))
            {
                return "wmv2";
            }
            else if (outputFormat.Equals("ogv"))
            {
                return "libtheora";
            }

            if (!RequiresVideoConversion())
            {
                return "copy";
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
            else if (outputFormat.Equals("asf"))
            {
                return "wmav2";
            }
            else if (outputFormat.Equals("wmv"))
            {
                return "wmav2";
            }
            else if (outputFormat.Equals("ogv"))
            {
                return "libvorbis";
            }

            // See if we can just copy the stream
            if (!RequiresAudioConversion(audioStream))
            {
                return "copy";
            }

            return "libvo_aacenc";
        }

        private int? GetNumAudioChannelsParam(string audioCodec, int libraryItemChannels)
        {
            if (libraryItemChannels > 2)
            {
                if (audioCodec.Equals("libvo_aacenc"))
                {
                    // libvo_aacenc currently only supports two channel output
                    return 2;
                }
                else if (audioCodec.Equals("wmav2"))
                {
                    // wmav2 currently only supports two channel output
                    return 2;
                }
            }

            return GetNumAudioChannelsParam(libraryItemChannels);
        }

        private bool RequiresVideoConversion()
        {
            // Check dimensions
            if (Width.HasValue)
            {
                if (Width.Value != LibraryItem.Width)
                {
                    return true;
                }
            }
            if (Height.HasValue)
            {
                if (Height.Value != LibraryItem.Height)
                {
                    return true;
                }
            }
            if (MaxWidth.HasValue)
            {
                if (MaxWidth.Value < LibraryItem.Width)
                {
                    return true;
                }
            }
            if (MaxHeight.HasValue)
            {
                if (MaxHeight.Value < LibraryItem.Height)
                {
                    return true;
                }
            }

            if (LibraryItem.Codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 || LibraryItem.Codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            return false;
        }

        private bool RequiresAudioConversion(AudioStream audio)
        {
            if (AudioChannels.HasValue)
            {
                if (audio.Channels > AudioChannels.Value)
                {
                    return true;
                }
            }

            if (audio.Codec.IndexOf("aac", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }
            if (audio.Codec.IndexOf("ac-3", StringComparison.OrdinalIgnoreCase) != -1 || audio.Codec.IndexOf("ac3", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }
            if (audio.Codec.IndexOf("mpeg", StringComparison.OrdinalIgnoreCase) != -1 || audio.Codec.IndexOf("mp3", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            return true;
        }

        private int? Height
        {
            get
            {
                string val = QueryString["height"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? Width
        {
            get
            {
                string val = QueryString["width"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? MaxHeight
        {
            get
            {
                string val = QueryString["maxheight"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? MaxWidth
        {
            get
            {
                string val = QueryString["maxwidth"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

    }
}
