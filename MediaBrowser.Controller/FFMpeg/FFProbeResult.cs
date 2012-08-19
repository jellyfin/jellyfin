using System.Collections.Generic;

namespace MediaBrowser.Controller.FFMpeg
{
    /// <summary>
    /// Provides a class that we can use to deserialize the ffprobe json output
    /// Sample output:
    /// http://stackoverflow.com/questions/7708373/get-ffmpeg-information-in-friendly-way
    /// </summary>
    public class FFProbeResult
    {
        public IEnumerable<MediaStream> streams { get; set; }
        public MediaFormat format { get; set; }
    }

    /// <summary>
    /// Represents a stream within the output
    /// A number of properties are commented out to improve deserialization performance
    /// Enable them as needed.
    /// </summary>
    public class MediaStream
    {
        public int index { get; set; }
        public string profile { get; set; }
        public string codec_name { get; set; }
        public string codec_long_name { get; set; }
        public string codec_type { get; set; }
        //public string codec_time_base { get; set; }
        //public string codec_tag { get; set; }
        //public string codec_tag_string { get; set; }
        //public string sample_fmt { get; set; }
        public string sample_rate { get; set; }
        public int channels { get; set; }
        //public int bits_per_sample { get; set; }
        //public string r_frame_rate { get; set; }
        //public string avg_frame_rate { get; set; }
        //public string time_base { get; set; }
        //public string start_time { get; set; }
        public string duration { get; set; }
        public string bit_rate { get; set; }

        public int width { get; set; }
        public int height { get; set; }
        //public int has_b_frames { get; set; }
        //public string sample_aspect_ratio { get; set; }
        //public string display_aspect_ratio { get; set; }
        //public string pix_fmt { get; set; }
        //public int level { get; set; }
        public Dictionary<string,string> tags { get; set; }
    }

    public class MediaFormat
    {
        public string filename { get; set; }
        public int nb_streams { get; set; }
        public string format_name { get; set; }
        public string format_long_name { get; set; }
        public string start_time { get; set; }
        public string duration { get; set; }
        public string size { get; set; }
        public string bit_rate { get; set; }
        public Dictionary<string, string> tags { get; set; }
    }
}
