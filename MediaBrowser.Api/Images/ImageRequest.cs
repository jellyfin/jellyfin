using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class ImageRequest
    /// </summary>
    public class ImageRequest : DeleteImageRequest
    {
        /// <summary>
        /// The max width
        /// </summary>
        public int? MaxWidth;
        /// <summary>
        /// The max height
        /// </summary>
        public int? MaxHeight;
        /// <summary>
        /// The width
        /// </summary>
        public int? Width;
        /// <summary>
        /// The height
        /// </summary>
        public int? Height;
        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public int? Quality { get; set; }
        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; set; }
    }

    /// <summary>
    /// Class DeleteImageRequest
    /// </summary>
    public class DeleteImageRequest
    {
        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        public ImageType Type { get; set; }
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public int? Index { get; set; }
    }
}
