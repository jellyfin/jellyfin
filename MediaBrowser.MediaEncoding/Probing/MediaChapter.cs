using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class MediaChapter.
    /// </summary>
    public class MediaChapter
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("time_base")]
        public string TimeBase { get; set; }

        [JsonPropertyName("start")]
        public long Start { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        [JsonPropertyName("end")]
        public long End { get; set; }

        [JsonPropertyName("end_time")]
        public string EndTime { get; set; }

        [JsonPropertyName("tags")]
        public IReadOnlyDictionary<string, string> Tags { get; set; }
    }
}
