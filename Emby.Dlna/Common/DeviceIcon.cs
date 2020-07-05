#pragma warning disable CS1591

using System.Globalization;

namespace Emby.Dlna.Common
{
    public class DeviceIcon
    {
        public string Url { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public int Width { get; set; } = 0;

        public int Height { get; set; } = 0;

        public string Depth { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Height}x{Width}";
        }
    }
}
