#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.MediaInfo
{
    public static class AudioCodec
    {
        public static string GetFriendlyName(string codec)
        {
            if (codec.Length == 0)
            {
                return codec;
            }

            if (string.Equals(codec, "ac3", StringComparison.OrdinalIgnoreCase))
            {
                return "Dolby Digital";
            }

            if (string.Equals(codec, "eac3", StringComparison.OrdinalIgnoreCase))
            {
                return "Dolby Digital+";
            }

            if (string.Equals(codec, "dca", StringComparison.OrdinalIgnoreCase))
            {
                return "DTS";
            }

            return codec.ToUpperInvariant();
        }
    }
}
