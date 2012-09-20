using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Net;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Supported output formats: mkv,m4v,mp4,asf,wmv,mov,webm,ogv,3gp,avi,ts,flv
    /// </summary>
    [Export(typeof(BaseHandler))]
    class VideoHandler : BaseMediaHandler<Video, VideoOutputFormats>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("video", request);
        }

        /// <summary>
        /// We can output these files directly, but we can't encode them
        /// </summary>
        protected override IEnumerable<VideoOutputFormats> UnsupportedOutputEncodingFormats
        {
            get
            {
                // mp4, 3gp, mov - muxer does not support non-seekable output
                // avi, mov, mkv, m4v - can't stream these when encoding. the player will try to download them completely before starting playback.
                // wmv - can't seem to figure out the output format name
                return new VideoOutputFormats[] { VideoOutputFormats.Mp4, VideoOutputFormats.ThreeGp, VideoOutputFormats.M4V, VideoOutputFormats.Mkv, VideoOutputFormats.Avi, VideoOutputFormats.Mov, VideoOutputFormats.Wmv };
            }
        }

        /// <summary>
        /// Determines whether or not we can just output the original file directly
        /// </summary>
        protected override bool RequiresConversion()
        {
            if (base.RequiresConversion())
            {
                return true;
            }

            // See if the video requires conversion
            if (RequiresVideoConversion())
            {
                return true;
            }

            // See if the audio requires conversion
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
        /// Translates the output file extension to the format param that follows "-f" on the ffmpeg command line
        /// </summary>
        private string GetFfMpegOutputFormat(VideoOutputFormats outputFormat)
        {
            if (outputFormat == VideoOutputFormats.Mkv)
            {
                return "matroska";
            }
            if (outputFormat == VideoOutputFormats.Ts)
            {
                return "mpegts";
            }
            if (outputFormat == VideoOutputFormats.Ogv)
            {
                return "ogg";
            }

            return outputFormat.ToString().ToLower();
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            VideoOutputFormats outputFormat = GetConversionOutputFormat();

            return string.Format("-i \"{0}\" -threads 0 {1} {2} -f {3} -",
                LibraryItem.Path,
                GetVideoArguments(outputFormat),
                GetAudioArguments(outputFormat),
                GetFfMpegOutputFormat(outputFormat)
                );
        }

        /// <summary>
        /// Gets video arguments to pass to ffmpeg
        /// </summary>
        private string GetVideoArguments(VideoOutputFormats outputFormat)
        {
            // Get the output codec name
            string codec = GetVideoCodec(outputFormat);

            string args = "-vcodec " + codec;

            // If we're encoding video, add additional params
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add resolution params, if specified
                if (Width.HasValue || Height.HasValue || MaxHeight.HasValue || MaxWidth.HasValue)
                {
                    Size size = DrawingUtils.Resize(LibraryItem.Width, LibraryItem.Height, Width, Height, MaxWidth, MaxHeight);

                    args += string.Format(" -s {0}x{1}", size.Width, size.Height);
                }
            }

            return args;
        }

        /// <summary>
        /// Gets audio arguments to pass to ffmpeg
        /// </summary>
        private string GetAudioArguments(VideoOutputFormats outputFormat)
        {
            AudioStream audioStream = (LibraryItem.AudioStreams ?? new List<AudioStream>()).FirstOrDefault();

            // If the video doesn't have an audio stream, return empty
            if (audioStream == null)
            {
                return string.Empty;
            }

            // Get the output codec name
            string codec = GetAudioCodec(audioStream, outputFormat);

            string args = "-acodec " + codec;

            // If we're encoding audio, add additional params
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // Add the number of audio channels
                int? channels = GetNumAudioChannelsParam(codec, audioStream.Channels);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                // Add the audio sample rate
                int? sampleRate = GetSampleRateParam(audioStream.SampleRate);

                if (sampleRate.HasValue)
                {
                    args += " -ar " + sampleRate.Value;
                }

            }

            return args;
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        private string GetVideoCodec(VideoOutputFormats outputFormat)
        {
            // Some output containers require specific codecs

            if (outputFormat == VideoOutputFormats.Webm)
            {
                // Per webm specification, it must be vpx
                return "libvpx";
            }
            if (outputFormat == VideoOutputFormats.Asf)
            {
                return "wmv2";
            }
            if (outputFormat == VideoOutputFormats.Wmv)
            {
                return "wmv2";
            }
            if (outputFormat == VideoOutputFormats.Ogv)
            {
                return "libtheora";
            }

            // Skip encoding when possible
            if (!RequiresVideoConversion())
            {
                return "copy";
            }

            return "libx264";
        }

        /// <summary>
        /// Gets the name of the output audio codec
        /// </summary>
        private string GetAudioCodec(AudioStream audioStream, VideoOutputFormats outputFormat)
        {
            // Some output containers require specific codecs

            if (outputFormat == VideoOutputFormats.Webm)
            {
                // Per webm specification, it must be vorbis
                return "libvorbis";
            }
            if (outputFormat == VideoOutputFormats.Asf)
            {
                return "wmav2";
            }
            if (outputFormat == VideoOutputFormats.Wmv)
            {
                return "wmav2";
            }
            if (outputFormat == VideoOutputFormats.Ogv)
            {
                return "libvorbis";
            }

            // Skip encoding when possible
            if (!RequiresAudioConversion(audioStream))
            {
                return "copy";
            }

            return "libvo_aacenc";
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        private int? GetNumAudioChannelsParam(string audioCodec, int libraryItemChannels)
        {
            if (libraryItemChannels > 2)
            {
                if (audioCodec.Equals("libvo_aacenc"))
                {
                    // libvo_aacenc currently only supports two channel output
                    return 2;
                }
                if (audioCodec.Equals("wmav2"))
                {
                    // wmav2 currently only supports two channel output
                    return 2;
                }
            }

            return GetNumAudioChannelsParam(libraryItemChannels);
        }

        /// <summary>
        /// Determines if the video stream requires encoding
        /// </summary>
        private bool RequiresVideoConversion()
        {
            // Check dimensions

            // If a specific width is required, validate that
            if (Width.HasValue)
            {
                if (Width.Value != LibraryItem.Width)
                {
                    return true;
                }
            }

            // If a specific height is required, validate that
            if (Height.HasValue)
            {
                if (Height.Value != LibraryItem.Height)
                {
                    return true;
                }
            }

            // If a max width is required, validate that
            if (MaxWidth.HasValue)
            {
                if (MaxWidth.Value < LibraryItem.Width)
                {
                    return true;
                }
            }

            // If a max height is required, validate that
            if (MaxHeight.HasValue)
            {
                if (MaxHeight.Value < LibraryItem.Height)
                {
                    return true;
                }
            }

            // If the codec is already h264, don't encode
            if (LibraryItem.Codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 || LibraryItem.Codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines if the audio stream requires encoding
        /// </summary>
        private bool RequiresAudioConversion(AudioStream audio)
        {

            // If the input stream has more audio channels than the client can handle, we need to encode
            if (AudioChannels.HasValue)
            {
                if (audio.Channels > AudioChannels.Value)
                {
                    return true;
                }
            }

            // Aac, ac-3 and mp3 are all pretty much universally supported. No need to encode them

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

        /// <summary>
        /// Gets the fixed output video height, in pixels
        /// </summary>
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

        /// <summary>
        /// Gets the fixed output video width, in pixels
        /// </summary>
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

        /// <summary>
        /// Gets the maximum output video height, in pixels
        /// </summary>
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

        /// <summary>
        /// Gets the maximum output video width, in pixels
        /// </summary>
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
