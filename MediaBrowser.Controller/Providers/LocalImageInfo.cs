#nullable disable

#pragma warning disable CS1591

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class LocalImageInfo
    {
        public FileSystemMetadata FileInfo { get; set; }

        public ImageType Type { get; set; }
    }
}
