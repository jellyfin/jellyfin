using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class MetadataOptions.
    /// </summary>
    public class MetadataOptions
    {
        public string ItemType { get; set; }

        public ImageOption[] ImageOptions { get; set; }

        public string[] DisabledMetadataSavers { get; set; }

        public MetadataOptions()
            : this(3, 1280)
        {
        }

        public MetadataOptions(int backdropLimit, int minBackdropWidth)
        {
            var imageOptions = new List<ImageOption>
            {
                new ImageOption
                {
                    Limit = backdropLimit,
                    MinWidth = minBackdropWidth,
                    Type = ImageType.Backdrop
                }
            };

            ImageOptions = imageOptions.ToArray();
            DisabledMetadataSavers = new string[] { };
        }

        public int GetLimit(ImageType type)
        {
            var option = ImageOptions.FirstOrDefault(i => i.Type == type);

            return option == null ? 1 : option.Limit;
        }

        public int GetMinWidth(ImageType type)
        {
            var option = ImageOptions.FirstOrDefault(i => i.Type == type);

            return option == null ? 0 : option.MinWidth;
        }

        public bool IsEnabled(ImageType type)
        {
            return GetLimit(type) > 0;
        }

        public bool IsMetadataSaverEnabled(string name)
        {
            return !DisabledMetadataSavers.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class ImageOption
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public ImageType Type { get; set; }
        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the minimum width.
        /// </summary>
        /// <value>The minimum width.</value>
        public int MinWidth { get; set; }

        public ImageOption()
        {
            Limit = 1;
        }
    }
}
