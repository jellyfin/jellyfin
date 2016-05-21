namespace MediaBrowser.Model.MediaInfo
{
    public class AudioCodec
    {
        public const string AAC = "aac";
        public const string MP3 = "mp3";
        public const string AC3 = "ac3";

        public static string GetFriendlyName(string codec)
        {
            if (string.IsNullOrEmpty(codec)) return "";

            switch (codec.ToLower())
            {
                case "ac3":
                    return "Dolby Digital";
                case "eac3":
                    return "Dolby Digital+";
                case "dca":
                    return "DTS";
                default:
                    return codec.ToUpper();
            }
        }
    }
}