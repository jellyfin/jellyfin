#pragma warning disable CS1591

using System.Globalization;

namespace MediaBrowser.Model.Dlna
{
    public class DeviceIcon
    {
        public string Url { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public int Width { get; set; }

        public int Height { get; set; }

        public string Depth { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}x{1}",
                Height,
                Width);
        }
    }
}
