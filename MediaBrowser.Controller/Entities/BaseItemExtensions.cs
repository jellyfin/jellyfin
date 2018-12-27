using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    public static class BaseItemExtensions
    {
        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>System.String.</returns>
        public static string GetImagePath(this BaseItem item, ImageType imageType)
        {
            return item.GetImagePath(imageType, 0);
        }

        public static bool HasImage(this BaseItem item, ImageType imageType)
        {
            return item.HasImage(imageType, 0);
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="file">The file.</param>
        public static void SetImagePath(this BaseItem item, ImageType imageType, FileSystemMetadata file)
        {
            item.SetImagePath(imageType, 0, file);
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="file">The file.</param>
        public static void SetImagePath(this BaseItem item, ImageType imageType, string file)
        {
            if (file.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
            {
                item.SetImage(new ItemImageInfo
                {
                    Path = file,
                    Type = imageType
                }, 0);
            }
            else
            {
                item.SetImagePath(imageType, BaseItem.FileSystem.GetFileInfo(file));
            }
        }
    }
}
