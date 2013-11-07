using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers
{
    public class CollectionFolderImageProvider : ImageFromMediaLocationProvider
    {
        public CollectionFolderImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager, fileSystem)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is CollectionFolder && item.LocationType == LocationType.FileSystem;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        protected override FileSystemInfo GetImage(BaseItem item, ItemResolveArgs args, string filenameWithoutExtension)
        {
            return item.ResolveArgs.PhysicalLocations
                .Select(i => GetImageFromLocation(i, filenameWithoutExtension))
                .FirstOrDefault(i => i != null);
        }

        protected override Guid GetFileSystemStamp(BaseItem item)
        {
            var files = item.ResolveArgs.PhysicalLocations
                .Select(i => new DirectoryInfo(i))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                .Where(i =>
                {
                    var ext = i.Extension;

                    return !string.IsNullOrEmpty(ext) &&
                        BaseItem.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                })
                .ToList();

            return GetFileSystemStamp(files);
        }
    }
}
