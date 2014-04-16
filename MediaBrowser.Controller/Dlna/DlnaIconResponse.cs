using MediaBrowser.Controller.Drawing;
using System;
using System.IO;

namespace MediaBrowser.Controller.Dlna
{
    public class DlnaIconResponse : IDisposable
    {
        public Stream Stream { get; set; }

        public ImageFormat Format { get; set; }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
        }
    }
}
