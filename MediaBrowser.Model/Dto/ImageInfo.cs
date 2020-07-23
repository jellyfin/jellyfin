#nullable disable
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ImageInfo.
    /// </summary>
    public class ImageInfo
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
        /// Gets or sets the image tag.
        /// </summary>
        public string ImageTag { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the blurhash.
        /// </summary>
        /// <value>The blurhash.</value>
        public string BlurHash { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public long Size { get; set; }
    }
}
