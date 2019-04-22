using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;

namespace Jellyfin.Controller.Providers
{
    public class LocalImageInfo
    {
        public FileSystemMetadata FileInfo { get; set; }
        public ImageType Type { get; set; }
    }
}
