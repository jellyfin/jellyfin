using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides images for all types by looking for standard images - folder, backdrop, logo, etc.
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class ImageFromMediaLocationProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return true;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.IsDirectory)
            {
                var baseItem = item as BaseItem;

                if (baseItem != null)
                {
                    return Task.Run(() => PopulateBaseItemImages(baseItem, args));
                }

                return Task.Run(() => PopulateImages(item, args));
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        private void PopulateImages(BaseEntity item, ItemResolveEventArgs args)
        {
            for (int i = 0; i < args.FileSystemChildren.Length; i++)
            {
                var file = args.FileSystemChildren[i];

                string filePath = file.Path;

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
            }
        }

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        private void PopulateBaseItemImages(BaseItem item, ItemResolveEventArgs args)
        {
            var backdropFiles = new List<string>();

            for (int i = 0; i < args.FileSystemChildren.Length; i++)
            {
                var file = args.FileSystemChildren[i];

                string filePath = file.Path;

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
                if (name.Equals("clearart", StringComparison.OrdinalIgnoreCase))
                {
                    item.ArtImagePath = filePath;
                }
                if (name.Equals("thumb", StringComparison.OrdinalIgnoreCase))
                {
                    item.ThumbnailImagePath = filePath;
                }
            }

            if (backdropFiles.Count > 0)
            {
                item.BackdropImagePaths = backdropFiles;
            }
        }

    }
}
