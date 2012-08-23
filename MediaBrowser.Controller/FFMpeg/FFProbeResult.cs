using System.Collections.Generic;
using ProtoBuf;

namespace MediaBrowser.Controller.FFMpeg
{
    /// <summary>
    /// Provides a class that we can use to deserialize the ffprobe json output
    /// Sample output:
    /// http://stackoverflow.com/questions/7708373/get-ffmpeg-information-in-friendly-way
    /// </summary>
    [ProtoContract]
    public class FFProbeResult
    {
        [ProtoMember(1)]
        public MediaStream[] streams { get; set; }

        [ProtoMember(2)]
        public MediaFormat format { get; set; }
    }

    /// <summary>
    /// Represents a stream within the output
    /// A number of properties are commented out to improve deserialization performance
    /// Enable them as needed.
    /// </summary>
    [ProtoContract]
    public class MediaStream
    {
        [ProtoMember(1)]
        public int index { get; set; }

        [ProtoMember(2)]
        public string profile { get; set; }

        [ProtoMember(3)]
        public string codec_name { get; set; }

        [ProtoMember(4)]
        public string codec_long_name { get; set; }

        [ProtoMember(5)]
        public string codec_type { get; set; }

        //public string codec_time_base { get; set; }
        //public string codec_tag { get; set; }
        //public string codec_tag_string { get; set; }
        //public string sample_fmt { get; set; }

        [ProtoMember(6)]
        public string sample_rate { get; set; }

        [ProtoMember(7)]
        public int channels { get; set; }

        //public int bits_per_sample { get; set; }
        //public string r_frame_rate { get; set; }

        [ProtoMember(8)]
        public string avg_frame_rate { get; set; }

        //public string time_base { get; set; }
        //public string start_time { get; set; }

        [ProtoMember(9)]
        public string duration { get; set; }

        [ProtoMember(10)]
        public string bit_rate { get; set; }

        [ProtoMember(11)]
        public int width { get; set; }

        [ProtoMember(12)]
        public int height { get; set; }

        //public int has_b_frames { get; set; }
        //public string sample_aspect_ratio { get; set; }

        [ProtoMember(13)]
        public string display_aspect_ratio { get; set; }

        //public string pix_fmt { get; set; }
        //public int level { get; set; }

        [ProtoMember(14)]
        public Dictionary<string, string> tags { get; set; }
   }

    [ProtoContract]
    public class MediaFormat
    {
        [ProtoMember(1)]
        public string filename { get; set; }

        [ProtoMember(2)]
        public int nb_streams { get; set; }

        [ProtoMember(3)]
        public string format_name { get; set; }

        [ProtoMember(4)]
        public string format_long_name { get; set; }

        [ProtoMember(5)]
        public string start_time { get; set; }

        [ProtoMember(6)]
        public string duration { get; set; }

        [ProtoMember(7)]
        public string size { get; set; }

        [ProtoMember(8)]
        public string bit_rate { get; set; }

        [ProtoMember(9)]
        public Dictionary<string, string> tags { get; set; }
    }
}
