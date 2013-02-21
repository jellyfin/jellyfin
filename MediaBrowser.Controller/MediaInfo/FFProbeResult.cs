using MediaBrowser.Model.Entities;
using ProtoBuf;
using System.Collections.Generic;

namespace MediaBrowser.Controller.MediaInfo
{
    /// <summary>
    /// Provides a class that we can use to deserialize the ffprobe json output
    /// Sample output:
    /// http://stackoverflow.com/questions/7708373/get-ffmpeg-information-in-friendly-way
    /// </summary>
    [ProtoContract]
    public class FFProbeResult
    {
        /// <summary>
        /// Gets or sets the streams.
        /// </summary>
        /// <value>The streams.</value>
        [ProtoMember(1)]
        public FFProbeMediaStreamInfo[] streams { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        [ProtoMember(2)]
        public FFProbeMediaFormatInfo format { get; set; }

        [ProtoMember(3)]
        public List<ChapterInfo> Chapters { get; set; }
    }

    /// <summary>
    /// Represents a stream within the output
    /// </summary>
    [ProtoContract]
    public class FFProbeMediaStreamInfo
    {
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [ProtoMember(1)]
        public int index { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        [ProtoMember(2)]
        public string profile { get; set; }

        /// <summary>
        /// Gets or sets the codec_name.
        /// </summary>
        /// <value>The codec_name.</value>
        [ProtoMember(3)]
        public string codec_name { get; set; }

        /// <summary>
        /// Gets or sets the codec_long_name.
        /// </summary>
        /// <value>The codec_long_name.</value>
        [ProtoMember(4)]
        public string codec_long_name { get; set; }

        /// <summary>
        /// Gets or sets the codec_type.
        /// </summary>
        /// <value>The codec_type.</value>
        [ProtoMember(5)]
        public string codec_type { get; set; }

        /// <summary>
        /// Gets or sets the sample_rate.
        /// </summary>
        /// <value>The sample_rate.</value>
        [ProtoMember(6)]
        public string sample_rate { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        [ProtoMember(7)]
        public int channels { get; set; }

        /// <summary>
        /// Gets or sets the avg_frame_rate.
        /// </summary>
        /// <value>The avg_frame_rate.</value>
        [ProtoMember(8)]
        public string avg_frame_rate { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        [ProtoMember(9)]
        public string duration { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        [ProtoMember(10)]
        public string bit_rate { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        [ProtoMember(11)]
        public int width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        [ProtoMember(12)]
        public int height { get; set; }

        /// <summary>
        /// Gets or sets the display_aspect_ratio.
        /// </summary>
        /// <value>The display_aspect_ratio.</value>
        [ProtoMember(13)]
        public string display_aspect_ratio { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [ProtoMember(14)]
        public Dictionary<string, string> tags { get; set; }

        /// <summary>
        /// Gets or sets the bits_per_sample.
        /// </summary>
        /// <value>The bits_per_sample.</value>
        [ProtoMember(17)]
        public int bits_per_sample { get; set; }

        /// <summary>
        /// Gets or sets the r_frame_rate.
        /// </summary>
        /// <value>The r_frame_rate.</value>
        [ProtoMember(18)]
        public string r_frame_rate { get; set; }

        /// <summary>
        /// Gets or sets the has_b_frames.
        /// </summary>
        /// <value>The has_b_frames.</value>
        [ProtoMember(19)]
        public int has_b_frames { get; set; }

        /// <summary>
        /// Gets or sets the sample_aspect_ratio.
        /// </summary>
        /// <value>The sample_aspect_ratio.</value>
        [ProtoMember(20)]
        public string sample_aspect_ratio { get; set; }

        /// <summary>
        /// Gets or sets the pix_fmt.
        /// </summary>
        /// <value>The pix_fmt.</value>
        [ProtoMember(21)]
        public string pix_fmt { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [ProtoMember(22)]
        public int level { get; set; }

        /// <summary>
        /// Gets or sets the time_base.
        /// </summary>
        /// <value>The time_base.</value>
        [ProtoMember(23)]
        public string time_base { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        [ProtoMember(24)]
        public string start_time { get; set; }

        /// <summary>
        /// Gets or sets the codec_time_base.
        /// </summary>
        /// <value>The codec_time_base.</value>
        [ProtoMember(25)]
        public string codec_time_base { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag.
        /// </summary>
        /// <value>The codec_tag.</value>
        [ProtoMember(26)]
        public string codec_tag { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag_string.
        /// </summary>
        /// <value>The codec_tag_string.</value>
        [ProtoMember(27)]
        public string codec_tag_string { get; set; }

        /// <summary>
        /// Gets or sets the sample_fmt.
        /// </summary>
        /// <value>The sample_fmt.</value>
        [ProtoMember(28)]
        public string sample_fmt { get; set; }

        /// <summary>
        /// Gets or sets the dmix_mode.
        /// </summary>
        /// <value>The dmix_mode.</value>
        [ProtoMember(29)]
        public string dmix_mode { get; set; }

        /// <summary>
        /// Gets or sets the start_pts.
        /// </summary>
        /// <value>The start_pts.</value>
        [ProtoMember(30)]
        public string start_pts { get; set; }

        /// <summary>
        /// Gets or sets the is_avc.
        /// </summary>
        /// <value>The is_avc.</value>
        [ProtoMember(31)]
        public string is_avc { get; set; }

        /// <summary>
        /// Gets or sets the nal_length_size.
        /// </summary>
        /// <value>The nal_length_size.</value>
        [ProtoMember(32)]
        public string nal_length_size { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_cmixlev.
        /// </summary>
        /// <value>The ltrt_cmixlev.</value>
        [ProtoMember(33)]
        public string ltrt_cmixlev { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_surmixlev.
        /// </summary>
        /// <value>The ltrt_surmixlev.</value>
        [ProtoMember(34)]
        public string ltrt_surmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_cmixlev.
        /// </summary>
        /// <value>The loro_cmixlev.</value>
        [ProtoMember(35)]
        public string loro_cmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_surmixlev.
        /// </summary>
        /// <value>The loro_surmixlev.</value>
        [ProtoMember(36)]
        public string loro_surmixlev { get; set; }

        /// <summary>
        /// Gets or sets the disposition.
        /// </summary>
        /// <value>The disposition.</value>
        [ProtoMember(37)]
        public Dictionary<string, string> disposition { get; set; }
    }

    /// <summary>
    /// Class MediaFormat
    /// </summary>
    [ProtoContract]
    public class FFProbeMediaFormatInfo
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        [ProtoMember(1)]
        public string filename { get; set; }

        /// <summary>
        /// Gets or sets the nb_streams.
        /// </summary>
        /// <value>The nb_streams.</value>
        [ProtoMember(2)]
        public int nb_streams { get; set; }

        /// <summary>
        /// Gets or sets the format_name.
        /// </summary>
        /// <value>The format_name.</value>
        [ProtoMember(3)]
        public string format_name { get; set; }

        /// <summary>
        /// Gets or sets the format_long_name.
        /// </summary>
        /// <value>The format_long_name.</value>
        [ProtoMember(4)]
        public string format_long_name { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        [ProtoMember(5)]
        public string start_time { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        [ProtoMember(6)]
        public string duration { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [ProtoMember(7)]
        public string size { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        [ProtoMember(8)]
        public string bit_rate { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [ProtoMember(9)]
        public Dictionary<string, string> tags { get; set; }
    }
}
