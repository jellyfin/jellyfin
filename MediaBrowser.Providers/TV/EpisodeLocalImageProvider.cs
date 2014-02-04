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

            return Directory.EnumerateFiles(parentPath, "*", SearchOption.AllDirectories)
                .Where(i =>
                {
                    if (BaseItem.SupportedImageExtensions.Contains(Path.GetExtension(i) ?? string.Empty))
                    {
                        var currentNameWithoutExtension = Path.GetFileNameWithoutExtension(i);

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
                    Path = i,
                    Type = ImageType.Primary
                })
                .ToList();
        }
    }
}
