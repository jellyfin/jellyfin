using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class MediaInfoResult.
    /// </summary>
    public class InternalMediaInfoResult
    {
        /// <summary>
        /// Gets or sets the streams.
        /// </summary>
        /// <value>The streams.</value>
        [JsonPropertyName("streams")]
        public IReadOnlyList<MediaStreamInfo> Streams { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        [JsonPropertyName("format")]
        public MediaFormatInfo Format { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        [JsonPropertyName("chapters")]
        public IReadOnlyList<MediaChapter> Chapters { get; set; }
    }
}
