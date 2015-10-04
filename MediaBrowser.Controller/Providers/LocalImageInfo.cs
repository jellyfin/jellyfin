using System.IO;
using CommonIO;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class LocalImageInfo
    {
        public FileSystemMetadata FileInfo { get; set; }
        public ImageType Type { get; set; }
    }
}