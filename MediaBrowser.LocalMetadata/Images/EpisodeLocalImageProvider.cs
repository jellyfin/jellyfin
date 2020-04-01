using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class EpisodeLocalLocalImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;

        public EpisodeLocalLocalImageProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name => "Local Images";

        public int Order => 0;

        public bool Supports(BaseItem item)
        {
            return item is Episode && item.SupportsLocalMetadata;
        }

        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var parentPath = Path.GetDirectoryName(item.Path);

            var parentPathFiles = directoryService.GetFiles(parentPath);

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);

            return GetFilesFromParentFolder(nameWithoutExtension, parentPathFiles);
        }

        private List<LocalImageInfo> GetFilesFromParentFolder(string filenameWithoutExtension, List<FileSystemMetadata> parentPathFiles)
        {
            var thumbName = filenameWithoutExtension + "-thumb";

            var list = new List<LocalImageInfo>(1);

            foreach (var i in parentPathFiles)
            {
                if (i.IsDirectory)
                {
                    continue;
                }

                if (BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    var currentNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(i);

                    if (string.Equals(filenameWithoutExtension, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new LocalImageInfo
                        {
                            FileInfo = i,
                            Type = ImageType.Primary
                        });
                    }

                    else if (string.Equals(thumbName, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new LocalImageInfo
                        {
                            FileInfo = i,
                            Type = ImageType.Primary
                        });
                    }
                }
            }
            return list;
        }
    }
}
