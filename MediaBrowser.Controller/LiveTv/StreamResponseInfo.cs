using System.IO;

namespace MediaBrowser.Controller.LiveTv
{
    public class StreamResponseInfo
    {
        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        /// <value>The stream.</value>
        public Stream Stream { get; set; }

        /// <summary>
        /// Gets or sets the type of the MIME.
        /// </summary>
        /// <value>The type of the MIME.</value>
        public string MimeType { get; set; }
    }
}
