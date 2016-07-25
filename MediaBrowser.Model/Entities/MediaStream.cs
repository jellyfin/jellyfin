using System.Collections.Generic;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using System.Diagnostics;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class MediaStream
    /// </summary>
    [DebuggerDisplay("StreamType = {Type}")]
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
        /// Gets or sets the comment.
        /// </summary>
        /// <value>The comment.</value>
        public string Comment { get; set; }

        public string TimeBase { get; set; }
        public string CodecTimeBase { get; set; }

        public string Title { get; set; }

        public string DisplayTitle
        {
            get
            {
                if (!string.IsNullOrEmpty(Title))
                {
                    return Title;
                }

                if (Type == MediaStreamType.Audio)
                {
                    List<string> attributes = new List<string>();

                    if (!string.IsNullOrEmpty(Language))
                    {
                        attributes.Add(StringHelper.FirstToUpper(Language));
                    }
                    if (!string.IsNullOrEmpty(Codec) && !StringHelper.EqualsIgnoreCase(Codec, "dca"))
                    {
                        attributes.Add(AudioCodec.GetFriendlyName(Codec));
                    } 
                    else if (!string.IsNullOrEmpty(Profile) && !StringHelper.EqualsIgnoreCase(Profile, "lc"))
                    {
                        attributes.Add(Profile);
                    }

                    if (!string.IsNullOrEmpty(ChannelLayout))
                    {
                        attributes.Add(ChannelLayout);
                    }
                    else if (Channels.HasValue)
                    {
                        attributes.Add(StringHelper.ToStringCultureInvariant(Channels.Value) + " ch");
                    }
                    if (IsDefault)
                    {
                        attributes.Add("Default");
                    }

                    return string.Join(" ", attributes.ToArray());
                }

                if (Type == MediaStreamType.Subtitle)
                {
                    List<string> attributes = new List<string>();

                    if (!string.IsNullOrEmpty(Language))
                    {
                        attributes.Add(StringHelper.FirstToUpper(Language));
                    }
                    if (IsDefault)
                    {
                        attributes.Add("Default");
                    }

                    if (IsForced)
                    {
                        attributes.Add("Forced");
                    }

                    string name = string.Join(" ", attributes.ToArray());

                    return name;
                }

                if (Type == MediaStreamType.Video)
                {

                }

                return null;
            }
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

            return StringHelper.IndexOfIgnoreCase(codec, "pgs") == -1 &&
                   StringHelper.IndexOfIgnoreCase(codec, "dvd") == -1 &&
                   StringHelper.IndexOfIgnoreCase(codec, "dvbsub") == -1 &&
                   !StringHelper.EqualsIgnoreCase(codec, "sub");
        }

        public bool SupportsSubtitleConversionTo(string codec)
        {
            if (!IsTextSubtitleStream)
            {
                return false;
            }

            // Can't convert from this 
            if (StringHelper.EqualsIgnoreCase(Codec, "ass"))
            {
                return false;
            }
            if (StringHelper.EqualsIgnoreCase(Codec, "ssa"))
            {
                return false;
            }

            // Can't convert to this 
            if (StringHelper.EqualsIgnoreCase(codec, "ass"))
            {
                return false;
            }
            if (StringHelper.EqualsIgnoreCase(codec, "ssa"))
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
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string ExternalId { get; set; }

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
