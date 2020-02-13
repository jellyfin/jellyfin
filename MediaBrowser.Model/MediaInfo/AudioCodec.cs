#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.MediaInfo
{
    public static class AudioCodec
    {
        public const string AAC = "aac";
        public const string MP3 = "mp3";
        public const string AC3 = "ac3";

        public static string GetFriendlyName(string codec)
        {
            if (string.IsNullOrEmpty(codec))
            {
                return string.Empty;
            }

            switch (codec.ToLowerInvariant())
            {
                case "ac3":
                    return "Dolby Digital";
                case "eac3":
                    return "Dolby Digital+";
                case "dca":
                    return "DTS";
                default:
                    return codec.ToUpperInvariant();
            }
        }
    }
}
