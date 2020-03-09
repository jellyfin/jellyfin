using System;
using System.IO;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.Providers
{
    public class DynamicImageResponse
    {
        public string Path { get; set; }

        public MediaProtocol Protocol { get; set; }

        public Stream Stream { get; set; }

        public ImageFormat Format { get; set; }

        public bool HasImage { get; set; }

        public void SetFormatFromMimeType(string mimeType)
        {
            if (mimeType.EndsWith("gif", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Gif;
            }
            else if (mimeType.EndsWith("bmp", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Bmp;
            }
            else if (mimeType.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Png;
            }
            else
            {
                Format = ImageFormat.Jpg;
            }
        }
    }
}
