using System.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class LocalImageInfo
    {
        public FileSystemInfo FileInfo { get; set; }
        public ImageType Type { get; set; }
    }
}