#nullable disable
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ImageOptions.
    /// </summary>
    public class ImageOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageOptions" /> class.
        /// </summary>
        public ImageOptions()
        {
            EnableImageEnhancers = true;
        }

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
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [crop whitespace].
        /// </summary>
        /// <value><c>null</c> if [crop whitespace] contains no value, <c>true</c> if [crop whitespace]; otherwise, <c>false</c>.</value>
        public bool? CropWhitespace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable image enhancers].
        /// </summary>
        /// <value><c>true</c> if [enable image enhancers]; otherwise, <c>false</c>.</value>
        public bool EnableImageEnhancers { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public ImageFormat? Format { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [add played indicator].
        /// </summary>
        /// <value><c>true</c> if [add played indicator]; otherwise, <c>false</c>.</value>
        public bool AddPlayedIndicator { get; set; }

        /// <summary>
        /// Gets or sets the percent played.
        /// </summary>
        /// <value>The percent played.</value>
        public int? PercentPlayed { get; set; }

        /// <summary>
        /// Gets or sets the un played count.
        /// </summary>
        /// <value>The un played count.</value>
        public int? UnPlayedCount { get; set; }

        /// <summary>
        /// Gets or sets the color of the background.
        /// </summary>
        /// <value>The color of the background.</value>
        public string BackgroundColor { get; set; }
    }
}
