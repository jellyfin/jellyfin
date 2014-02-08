using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeLocalLocalImageProvider : ILocalImageFileProvider
    {
        public string Name
        {
            get { return "Local Images"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Episode && item.LocationType == LocationType.FileSystem;
        }

        public List<LocalImageInfo> GetImages(IHasImages item)
        {
            var file = GetFile(item);

            var list = new List<LocalImageInfo>();

            if (file != null)
            {
                list.Add(new LocalImageInfo
                {
                    FileInfo = file,
                    Type = ImageType.Primary
                });
            }

            return list;
        }

        private FileInfo GetFile(IHasImages item)
        {
            var parentPath = Path.GetDirectoryName(item.Path);

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);
            var thumbName = nameWithoutExtension + "-thumb";

            var path = Path.Combine(parentPath, thumbName + ".jpg");
            var fileInfo = new FileInfo(path);

            if (fileInfo.Exists)
            {
                return fileInfo;
            }

            path = Path.Combine(parentPath, "metadata", nameWithoutExtension + ".jpg");
            fileInfo = new FileInfo(path);

            if (fileInfo.Exists)
            {
                return fileInfo;
            }

            return null;
        }
    }
}
