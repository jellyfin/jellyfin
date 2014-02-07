using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeLocalImageProvider : IImageFileProvider
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
            var parentPath = Path.GetDirectoryName(item.Path);

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);
            var thumbName = nameWithoutExtension + "-thumb";

            return new DirectoryInfo(parentPath).EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(i =>
                {
                    if (BaseItem.SupportedImageExtensions.Contains(i.Extension))
                    {
                        var currentNameWithoutExtension = Path.GetFileNameWithoutExtension(i.Name);

                        if (string.Equals(nameWithoutExtension, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        if (string.Equals(thumbName, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                })
                .Select(i => new LocalImageInfo
                {
                    FileInfo = i,
                    Type = ImageType.Primary
                })
                .ToList();
        }
    }
}
