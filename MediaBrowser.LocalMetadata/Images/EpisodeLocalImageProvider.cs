using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    /// <summary>
    /// Episode local image provider.
    /// </summary>
    public class EpisodeLocalImageProvider : ILocalImageProvider, IHasOrder
    {
        /// <inheritdoc />
        public string Name => "Local Images";

        /// <inheritdoc />
        public int Order => 0;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Episode && item.SupportsLocalMetadata;
        }

        /// <inheritdoc />
        public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var parentPath = Path.GetDirectoryName(item.Path);
            if (parentPath == null)
            {
                return Enumerable.Empty<LocalImageInfo>();
            }

            var parentPathFiles = directoryService.GetFiles(parentPath);

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path.AsSpan());

            return GetFilesFromParentFolder(nameWithoutExtension, parentPathFiles);
        }

        private List<LocalImageInfo> GetFilesFromParentFolder(ReadOnlySpan<char> filenameWithoutExtension, List<FileSystemMetadata> parentPathFiles)
        {
            var thumbName = string.Concat(filenameWithoutExtension, "-thumb");

            var list = new List<LocalImageInfo>(1);

            foreach (var i in parentPathFiles)
            {
                if (i.IsDirectory)
                {
                    continue;
                }

                if (BaseItem.SupportedImageExtensions.Contains(i.Extension.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    var currentNameWithoutExtension = Path.GetFileNameWithoutExtension(i.FullName.AsSpan());

                    if (filenameWithoutExtension.Equals(currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new LocalImageInfo { FileInfo = i, Type = ImageType.Primary });
                    }
                    else if (currentNameWithoutExtension.Equals(thumbName, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new LocalImageInfo { FileInfo = i, Type = ImageType.Primary });
                    }
                }
            }

            return list;
        }
    }
}
