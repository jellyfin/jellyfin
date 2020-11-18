namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceIcon" />.
    /// </summary>
    public class DeviceIcon
    {
        /// <summary>
        /// Gets or sets the Url of the icon.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MimeType.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Width of the icon.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the Height of the icon.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the Depth of the icon.
        /// </summary>
        public string Depth { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Height}x{Width}";
        }
    }
}
