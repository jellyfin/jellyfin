using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Json.Converters;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Represents a stream within the output.
    /// </summary>
    public class MediaStreamInfo
    {
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        [JsonPropertyName("profile")]
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the codec_name.
        /// </summary>
        /// <value>The codec_name.</value>
        [JsonPropertyName("codec_name")]
        public string CodecName { get; set; }

        /// <summary>
        /// Gets or sets the codec_long_name.
        /// </summary>
        /// <value>The codec_long_name.</value>
        [JsonPropertyName("codec_long_name")]
        public string CodecLongName { get; set; }

        /// <summary>
        /// Gets or sets the codec_type.
        /// </summary>
        /// <value>The codec_type.</value>
        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; }

        /// <summary>
        /// Gets or sets the sample_rate.
        /// </summary>
        /// <value>The sample_rate.</value>
        [JsonPropertyName("sample_rate")]
        public string SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        [JsonPropertyName("channels")]
        public int Channels { get; set; }

        /// <summary>
        /// Gets or sets the channel_layout.
        /// </summary>
        /// <value>The channel_layout.</value>
        [JsonPropertyName("channel_layout")]
        public string ChannelLayout { get; set; }

        /// <summary>
        /// Gets or sets the avg_frame_rate.
        /// </summary>
        /// <value>The avg_frame_rate.</value>
        [JsonPropertyName("avg_frame_rate")]
        public string AverageFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        [JsonPropertyName("bit_rate")]
        public string BitRate { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        [JsonPropertyName("width")]
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the refs.
        /// </summary>
        /// <value>The refs.</value>
        [JsonPropertyName("refs")]
        public int Refs { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        [JsonPropertyName("height")]
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the display_aspect_ratio.
        /// </summary>
        /// <value>The display_aspect_ratio.</value>
        [JsonPropertyName("display_aspect_ratio")]
        public string DisplayAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [JsonPropertyName("tags")]
        public IReadOnlyDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the bits_per_sample.
        /// </summary>
        /// <value>The bits_per_sample.</value>
        [JsonPropertyName("bits_per_sample")]
        public int BitsPerSample { get; set; }

        /// <summary>
        /// Gets or sets the bits_per_raw_sample.
        /// </summary>
        /// <value>The bits_per_raw_sample.</value>
        [JsonPropertyName("bits_per_raw_sample")]
        [JsonConverter(typeof(JsonInt32Converter))]
        public int BitsPerRawSample { get; set; }

        /// <summary>
        /// Gets or sets the r_frame_rate.
        /// </summary>
        /// <value>The r_frame_rate.</value>
        [JsonPropertyName("r_frame_rate")]
        public string RFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the has_b_frames.
        /// </summary>
        /// <value>The has_b_frames.</value>
        [JsonPropertyName("has_b_frames")]
        public int HasBFrames { get; set; }

        /// <summary>
        /// Gets or sets the sample_aspect_ratio.
        /// </summary>
        /// <value>The sample_aspect_ratio.</value>
        [JsonPropertyName("sample_aspect_ratio")]
        public string SampleAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the pix_fmt.
        /// </summary>
        /// <value>The pix_fmt.</value>
        [JsonPropertyName("pix_fmt")]
        public string PixelFormat { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonPropertyName("level")]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the time_base.
        /// </summary>
        /// <value>The time_base.</value>
        [JsonPropertyName("time_base")]
        public string TimeBase { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the codec_time_base.
        /// </summary>
        /// <value>The codec_time_base.</value>
        [JsonPropertyName("codec_time_base")]
        public string CodecTimeBase { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag.
        /// </summary>
        /// <value>The codec_tag.</value>
        [JsonPropertyName("codec_tag")]
        public string CodecTag { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag_string.
        /// </summary>
        /// <value>The codec_tag_string.</value>
        [JsonPropertyName("codec_tag_string")]
        public string CodecTagString { get; set; }

        /// <summary>
        /// Gets or sets the sample_fmt.
        /// </summary>
        /// <value>The sample_fmt.</value>
        [JsonPropertyName("sample_fmt")]
        public string SampleFmt { get; set; }

        /// <summary>
        /// Gets or sets the dmix_mode.
        /// </summary>
        /// <value>The dmix_mode.</value>
        [JsonPropertyName("dmix_mode")]
        public string DmixMode { get; set; }

        /// <summary>
        /// Gets or sets the start_pts.
        /// </summary>
        /// <value>The start_pts.</value>
        [JsonPropertyName("start_pts")]
        public long StartPts { get; set; }

        /// <summary>
        /// Gets or sets the is_avc.
        /// </summary>
        /// <value>The is_avc.</value>
        [JsonPropertyName("is_avc")]
        public string IsAvc { get; set; }

        /// <summary>
        /// Gets or sets the nal_length_size.
        /// </summary>
        /// <value>The nal_length_size.</value>
        [JsonPropertyName("nal_length_size")]
        public string NalLengthSize { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_cmixlev.
        /// </summary>
        /// <value>The ltrt_cmixlev.</value>
        [JsonPropertyName("ltrt_cmixlev")]
        public string LtrtCmixlev { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_surmixlev.
        /// </summary>
        /// <value>The ltrt_surmixlev.</value>
        [JsonPropertyName("ltrt_surmixlev")]
        public string LtrtSurmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_cmixlev.
        /// </summary>
        /// <value>The loro_cmixlev.</value>
        [JsonPropertyName("loro_cmixlev")]
        public string LoroCmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_surmixlev.
        /// </summary>
        /// <value>The loro_surmixlev.</value>
        [JsonPropertyName("loro_surmixlev")]
        public string LoroSurmixlev { get; set; }

        [JsonPropertyName("field_order")]
        public string FieldOrder { get; set; }

        /// <summary>
        /// Gets or sets the disposition.
        /// </summary>
        /// <value>The disposition.</value>
        [JsonPropertyName("disposition")]
        public IReadOnlyDictionary<string, int> Disposition { get; set; }

        /// <summary>
        /// Gets or sets the color transfer.
        /// </summary>
        /// <value>The color transfer.</value>
        [JsonPropertyName("color_transfer")]
        public string ColorTransfer { get; set; }

        /// <summary>
        /// Gets or sets the color primaries.
        /// </summary>
        /// <value>The color primaries.</value>
        [JsonPropertyName("color_primaries")]
        public string ColorPrimaries { get; set; }
    }
}
