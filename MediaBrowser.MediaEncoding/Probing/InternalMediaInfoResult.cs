using System.Collections.Generic;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class MediaInfoResult
    /// </summary>
    public class InternalMediaInfoResult
    {
        /// <summary>
        /// Gets or sets the streams.
        /// </summary>
        /// <value>The streams.</value>
        public MediaStreamInfo[] streams { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public MediaFormatInfo format { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        public MediaChapter[] Chapters { get; set; }
    }

    public class MediaChapter
    {
        public int id { get; set; }
        public string time_base { get; set; }
        public long start { get; set; }
        public string start_time { get; set; }
        public long end { get; set; }
        public string end_time { get; set; }
        public Dictionary<string, string> tags { get; set; }
    }

    /// <summary>
    /// Represents a stream within the output
    /// </summary>
    public class MediaStreamInfo
    {
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public int index { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string profile { get; set; }

        /// <summary>
        /// Gets or sets the codec_name.
        /// </summary>
        /// <value>The codec_name.</value>
        public string codec_name { get; set; }

        /// <summary>
        /// Gets or sets the codec_long_name.
        /// </summary>
        /// <value>The codec_long_name.</value>
        public string codec_long_name { get; set; }

        /// <summary>
        /// Gets or sets the codec_type.
        /// </summary>
        /// <value>The codec_type.</value>
        public string codec_type { get; set; }

        /// <summary>
        /// Gets or sets the sample_rate.
        /// </summary>
        /// <value>The sample_rate.</value>
        public string sample_rate { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        public int channels { get; set; }

        /// <summary>
        /// Gets or sets the channel_layout.
        /// </summary>
        /// <value>The channel_layout.</value>
        public string channel_layout { get; set; }

        /// <summary>
        /// Gets or sets the avg_frame_rate.
        /// </summary>
        /// <value>The avg_frame_rate.</value>
        public string avg_frame_rate { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public string duration { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        public string bit_rate { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int width { get; set; }

        /// <summary>
        /// Gets or sets the refs.
        /// </summary>
        /// <value>The refs.</value>
        public int refs { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int height { get; set; }

        /// <summary>
        /// Gets or sets the display_aspect_ratio.
        /// </summary>
        /// <value>The display_aspect_ratio.</value>
        public string display_aspect_ratio { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public Dictionary<string, string> tags { get; set; }

        /// <summary>
        /// Gets or sets the bits_per_sample.
        /// </summary>
        /// <value>The bits_per_sample.</value>
        public int bits_per_sample { get; set; }

        /// <summary>
        /// Gets or sets the bits_per_raw_sample.
        /// </summary>
        /// <value>The bits_per_raw_sample.</value>
        public int bits_per_raw_sample { get; set; }
        
        /// <summary>
        /// Gets or sets the r_frame_rate.
        /// </summary>
        /// <value>The r_frame_rate.</value>
        public string r_frame_rate { get; set; }

        /// <summary>
        /// Gets or sets the has_b_frames.
        /// </summary>
        /// <value>The has_b_frames.</value>
        public int has_b_frames { get; set; }

        /// <summary>
        /// Gets or sets the sample_aspect_ratio.
        /// </summary>
        /// <value>The sample_aspect_ratio.</value>
        public string sample_aspect_ratio { get; set; }

        /// <summary>
        /// Gets or sets the pix_fmt.
        /// </summary>
        /// <value>The pix_fmt.</value>
        public string pix_fmt { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public int level { get; set; }

        /// <summary>
        /// Gets or sets the time_base.
        /// </summary>
        /// <value>The time_base.</value>
        public string time_base { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        public string start_time { get; set; }

        /// <summary>
        /// Gets or sets the codec_time_base.
        /// </summary>
        /// <value>The codec_time_base.</value>
        public string codec_time_base { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag.
        /// </summary>
        /// <value>The codec_tag.</value>
        public string codec_tag { get; set; }

        /// <summary>
        /// Gets or sets the codec_tag_string.
        /// </summary>
        /// <value>The codec_tag_string.</value>
        public string codec_tag_string { get; set; }

        /// <summary>
        /// Gets or sets the sample_fmt.
        /// </summary>
        /// <value>The sample_fmt.</value>
        public string sample_fmt { get; set; }

        /// <summary>
        /// Gets or sets the dmix_mode.
        /// </summary>
        /// <value>The dmix_mode.</value>
        public string dmix_mode { get; set; }

        /// <summary>
        /// Gets or sets the start_pts.
        /// </summary>
        /// <value>The start_pts.</value>
        public string start_pts { get; set; }

        /// <summary>
        /// Gets or sets the is_avc.
        /// </summary>
        /// <value>The is_avc.</value>
        public string is_avc { get; set; }

        /// <summary>
        /// Gets or sets the nal_length_size.
        /// </summary>
        /// <value>The nal_length_size.</value>
        public string nal_length_size { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_cmixlev.
        /// </summary>
        /// <value>The ltrt_cmixlev.</value>
        public string ltrt_cmixlev { get; set; }

        /// <summary>
        /// Gets or sets the ltrt_surmixlev.
        /// </summary>
        /// <value>The ltrt_surmixlev.</value>
        public string ltrt_surmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_cmixlev.
        /// </summary>
        /// <value>The loro_cmixlev.</value>
        public string loro_cmixlev { get; set; }

        /// <summary>
        /// Gets or sets the loro_surmixlev.
        /// </summary>
        /// <value>The loro_surmixlev.</value>
        public string loro_surmixlev { get; set; }

        /// <summary>
        /// Gets or sets the disposition.
        /// </summary>
        /// <value>The disposition.</value>
        public Dictionary<string, string> disposition { get; set; }
    }

    /// <summary>
    /// Class MediaFormat
    /// </summary>
    public class MediaFormatInfo
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        public string filename { get; set; }

        /// <summary>
        /// Gets or sets the nb_streams.
        /// </summary>
        /// <value>The nb_streams.</value>
        public int nb_streams { get; set; }

        /// <summary>
        /// Gets or sets the format_name.
        /// </summary>
        /// <value>The format_name.</value>
        public string format_name { get; set; }

        /// <summary>
        /// Gets or sets the format_long_name.
        /// </summary>
        /// <value>The format_long_name.</value>
        public string format_long_name { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        public string start_time { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public string duration { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public string size { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        public string bit_rate { get; set; }

        /// <summary>
        /// Gets or sets the probe_score.
        /// </summary>
        /// <value>The probe_score.</value>
        public int probe_score { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public Dictionary<string, string> tags { get; set; }
    }
}
