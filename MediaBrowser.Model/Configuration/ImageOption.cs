#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
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
