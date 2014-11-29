using System;
using System.IO;
using MediaBrowser.Model.Drawing;

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
