
using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ImageOptions
    /// </summary>
    public class ImageOptions
    {
        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        public ImageType ImageType { get; set; }

        /// <summary>
        /// Gets or sets the index of the image.
        /// </summary>
        /// <value>The index of the image.</value>
        public int? ImageIndex { get; set; }
        
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width of the max.
        /// </summary>
        /// <value>The width of the max.</value>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the max.
        /// </summary>
        /// <value>The height of the max.</value>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public int? Quality { get; set; }

        /// <summary>
        /// Gets or sets the image tag.
        /// If set this will result in strong, unconditional response caching
        /// </summary>
        /// <value>The hash.</value>
        public Guid? Tag { get; set; }
    }
}
