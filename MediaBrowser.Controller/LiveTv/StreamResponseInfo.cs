using System.IO;
using MediaBrowser.Model.Drawing;

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
        public ImageFormat Format { get; set; }
    }
}
