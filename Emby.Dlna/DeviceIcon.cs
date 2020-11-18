using System.Globalization;

namespace Emby.Dlna.Common
{
    /// <summary>
    /// Defines the <see cref="DeviceIcon" />.
    /// </summary>
    public class DeviceIcon
    {
        /// <summary>
        /// Gets or sets the Url.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MimeType.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the Height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the Depth.
        /// </summary>
        public string Depth { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", Height, Width);
        }
    }
}
