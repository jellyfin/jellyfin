using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class ImageFromMediaLocationProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is BaseItem;
        }
        
        public override Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            return Task.Run(() =>
            {
                if (args.IsFolder)
                {
                    PopulateImages(item as BaseItem, args);
                }
            });
        }

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        private void PopulateImages(BaseItem item, ItemResolveEventArgs args)
        {
            List<string> backdropFiles = new List<string>();

            foreach (KeyValuePair<string, FileAttributes> file in args.FileSystemChildren)
            {
                if (file.Value.HasFlag(FileAttributes.Directory))
                {
                    continue;
                }

                string filePath = file.Key;

                string ext = Path.GetExtension(filePath);

                // Only support png and jpg files
                if (!ext.EndsWith("png", StringComparison.OrdinalIgnoreCase) && !ext.EndsWith("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(filePath);

                if (name.Equals("folder", StringComparison.OrdinalIgnoreCase))
                {
                    item.PrimaryImagePath = filePath;
                }
                else if (name.StartsWith("backdrop", StringComparison.OrdinalIgnoreCase))
                {
                    backdropFiles.Add(filePath);
                }
                if (name.Equals("logo", StringComparison.OrdinalIgnoreCase))
                {
                    item.LogoImagePath = filePath;
                }
                if (name.Equals("banner", StringComparison.OrdinalIgnoreCase))
                {
                    item.BannerImagePath = filePath;
                }
                if (name.Equals("art", StringComparison.OrdinalIgnoreCase))
                {
                    item.ArtImagePath = filePath;
                }
                if (name.Equals("thumb", StringComparison.OrdinalIgnoreCase))
                {
                    item.ThumbnailImagePath = filePath;
                }
            }

            if (backdropFiles.Any())
            {
                item.BackdropImagePaths = backdropFiles;
            }
        }

    }
}
