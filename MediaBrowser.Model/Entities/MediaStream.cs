#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class MediaStream
    /// </summary>
    public class MediaStream
    {
        /// <summary>
        /// Gets or sets the codec.
        /// </summary>
        /// <value>The codec.</value>
        public string Codec { get; set; }

        /// <summary>
        /// Gets or sets the codec tag.
        /// </summary>
        /// <value>The codec tag.</value>
        public string CodecTag { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the color transfer.
        /// </summary>
        /// <value>The color transfer.</value>
        public string ColorTransfer { get; set; }

        /// <summary>
        /// Gets or sets the color primaries.
        /// </summary>
        /// <value>The color primaries.</value>
        public string ColorPrimaries { get; set; }

        /// <summary>
        /// Gets or sets the color space.
        /// </summary>
        /// <value>The color space.</value>
        public string ColorSpace { get; set; }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>The comment.</value>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the time base.
        /// </summary>
        /// <value>The time base.</value>
        public string TimeBase { get; set; }

        /// <summary>
        /// Gets or sets the codec time base.
        /// </summary>
        /// <value>The codec time base.</value>
        public string CodecTimeBase { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the video range.
        /// </summary>
        /// <value>The video range.</value>
        public string VideoRange
        {
            get
            {
                if (Type != MediaStreamType.Video)
                {
                    return null;
                }

                var colorTransfer = ColorTransfer;

                if (string.Equals(colorTransfer, "smpte2084", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(colorTransfer, "arib-std-b67", StringComparison.OrdinalIgnoreCase))
                {
                    return "HDR";
                }

                return "SDR";
            }
        }

        public string localizedUndefined { get; set; }

        public string localizedDefault { get; set; }

        public string localizedForced { get; set; }

        public string DisplayTitle
        {
            get
            {
                if (Type == MediaStreamType.Audio)
                {
                    //if (!string.IsNullOrEmpty(Title))
                    //{
                    //    return AddLanguageIfNeeded(Title);
                    //}

                    var attributes = new List<string>();

                    if (!string.IsNullOrEmpty(Language))
                    {
                        attributes.Add(StringHelper.FirstToUpper(Language));
                    }
                    if (!string.IsNullOrEmpty(Codec) && !string.Equals(Codec, "dca", StringComparison.OrdinalIgnoreCase))
                    {
                        attributes.Add(AudioCodec.GetFriendlyName(Codec));
                    }
                    else if (!string.IsNullOrEmpty(Profile) && !string.Equals(Profile, "lc", StringComparison.OrdinalIgnoreCase))
                    {
                        attributes.Add(Profile);
                    }

                    if (!string.IsNullOrEmpty(ChannelLayout))
                    {
                        attributes.Add(ChannelLayout);
                    }
                    else if (Channels.HasValue)
                    {
                        attributes.Add(Channels.Value.ToString(CultureInfo.InvariantCulture) + " ch");
                    }
                    if (IsDefault)
                    {
                        attributes.Add("Default");
                    }

                    return string.Join(" ", attributes);
                }

                if (Type == MediaStreamType.Video)
                {
                    var attributes = new List<string>();

                    var resolutionText = GetResolutionText();

                    if (!string.IsNullOrEmpty(resolutionText))
                    {
                        attributes.Add(resolutionText);
                    }

                    if (!string.IsNullOrEmpty(Codec))
                    {
                        attributes.Add(Codec.ToUpperInvariant());
                    }

                    return string.Join(" ", attributes);
                }

                if (Type == MediaStreamType.Subtitle)
                {

                    var attributes = new List<string>();

                    if (!string.IsNullOrEmpty(Language))
                    {
                        attributes.Add(StringHelper.FirstToUpper(Language));
                    }
                    else
                    {
                        attributes.Add(string.IsNullOrEmpty(localizedUndefined) ? "Und" : localizedUndefined);
                    }

                    if (IsDefault)
                    {
                        attributes.Add(string.IsNullOrEmpty(localizedDefault) ? "Default" : localizedDefault);
                    }

                    if (IsForced)
                    {
                        attributes.Add(string.IsNullOrEmpty(localizedForced) ? "Forced" : localizedForced);
                    }

                    if (!string.IsNullOrEmpty(Title))
                    {
                        return attributes.AsEnumerable()
                        // keep Tags that are not already in Title
                        .Where(tag => Title.IndexOf(tag, StringComparison.OrdinalIgnoreCase) == -1)
                        // attributes concatenation, starting with Title
                        .Aggregate(new StringBuilder(Title), (builder, attr) => builder.Append(" - ").Append(attr))
                        .ToString();
                    }

                    return string.Join(" - ", attributes.ToArray());
                }

                if (Type == MediaStreamType.Video)
                {

                }

                return null;
            }
        }

        private string GetResolutionText()
        {
            var i = this;

            if (i.Width.HasValue && i.Height.HasValue)
            {
                var width = i.Width.Value;
                var height = i.Height.Value;

                if (width >= 3800 || height >= 2000)
                {
                    return "4K";
                }
                if (width >= 2500)
                {
                    if (i.IsInterlaced)
                    {
                        return "1440i";
                    }
                    return "1440p";
                }
                if (width >= 1900 || height >= 1000)
                {
                    if (i.IsInterlaced)
                    {
                        return "1080i";
                    }
                    return "1080p";
                }
                if (width >= 1260 || height >= 700)
                {
                    if (i.IsInterlaced)
                    {
                        return "720i";
                    }
                    return "720p";
                }
                if (width >= 700 || height >= 440)
                {

                    if (i.IsInterlaced)
                    {
                        return "480i";
                    }
                    return "480p";
                }

                return "SD";
            }
            return null;
        }

        public string NalLengthSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is interlaced.
        /// </summary>
        /// <value><c>true</c> if this instance is interlaced; otherwise, <c>false</c>.</value>
        public bool IsInterlaced { get; set; }

        public bool? IsAVC { get; set; }

        /// <summary>
        /// Gets or sets the channel layout.
        /// </summary>
        /// <value>The channel layout.</value>
        public string ChannelLayout { get; set; }

        /// <summary>
        /// Gets or sets the bit rate.
        /// </summary>
        /// <value>The bit rate.</value>
        public int? BitRate { get; set; }

        /// <summary>
        /// Gets or sets the bit depth.
        /// </summary>
        /// <value>The bit depth.</value>
        public int? BitDepth { get; set; }

        /// <summary>
        /// Gets or sets the reference frames.
        /// </summary>
        /// <value>The reference frames.</value>
        public int? RefFrames { get; set; }

        /// <summary>
        /// Gets or sets the length of the packet.
        /// </summary>
        /// <value>The length of the packet.</value>
        public int? PacketLength { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        public int? Channels { get; set; }

        /// <summary>
        /// Gets or sets the sample rate.
        /// </summary>
        /// <value>The sample rate.</value>
        public int? SampleRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is default.
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is forced.
        /// </summary>
        /// <value><c>true</c> if this instance is forced; otherwise, <c>false</c>.</value>
        public bool IsForced { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the average frame rate.
        /// </summary>
        /// <value>The average frame rate.</value>
        public float? AverageFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the real frame rate.
        /// </summary>
        /// <value>The real frame rate.</value>
        public float? RealFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public MediaStreamType Type { get; set; }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        public string AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is external.
        /// </summary>
        /// <value><c>true</c> if this instance is external; otherwise, <c>false</c>.</value>
        public bool IsExternal { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>The method.</value>
        public SubtitleDeliveryMethod? DeliveryMethod { get; set; }
        /// <summary>
        /// Gets or sets the delivery URL.
        /// </summary>
        /// <value>The delivery URL.</value>
        public string DeliveryUrl { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is external URL.
        /// </summary>
        /// <value><c>null</c> if [is external URL] contains no value, <c>true</c> if [is external URL]; otherwise, <c>false</c>.</value>
        public bool? IsExternalUrl { get; set; }

        public bool IsTextSubtitleStream
        {
            get
            {
                if (Type != MediaStreamType.Subtitle) return false;

                if (string.IsNullOrEmpty(Codec) && !IsExternal)
                {
                    return false;
                }

                return IsTextFormat(Codec);
            }
        }

        public static bool IsTextFormat(string format)
        {
            string codec = format ?? string.Empty;

            // sub = external .sub file

            return codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) == -1 &&
                   codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) == -1 &&
                   codec.IndexOf("dvbsub", StringComparison.OrdinalIgnoreCase) == -1 &&
                   !string.Equals(codec, "sub", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(codec, "dvb_subtitle", StringComparison.OrdinalIgnoreCase);
        }

        public bool SupportsSubtitleConversionTo(string toCodec)
        {
            if (!IsTextSubtitleStream)
            {
                return false;
            }

            var fromCodec = Codec;

            // Can't convert from this
            if (string.Equals(fromCodec, "ass", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (string.Equals(fromCodec, "ssa", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Can't convert to this
            if (string.Equals(toCodec, "ass", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (string.Equals(toCodec, "ssa", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [supports external stream].
        /// </summary>
        /// <value><c>true</c> if [supports external stream]; otherwise, <c>false</c>.</value>
        public bool SupportsExternalStream { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the pixel format.
        /// </summary>
        /// <value>The pixel format.</value>
        public string PixelFormat { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public double? Level { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is anamorphic.
        /// </summary>
        /// <value><c>true</c> if this instance is anamorphic; otherwise, <c>false</c>.</value>
        public bool? IsAnamorphic { get; set; }
    }
}
