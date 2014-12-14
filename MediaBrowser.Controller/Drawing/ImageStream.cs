using MediaBrowser.Model.Drawing;
using System;
using System.IO;

namespace MediaBrowser.Controller.Drawing
{
    public class ImageStream : IDisposable
    {
        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        /// <value>The stream.</value>
        public Stream Stream { get; set; }
        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public ImageFormat Format { get; set; }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
            }
        }
    }
}
