using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public static class TvdbUtils
    {
        public const string TvdbApiKey = "OG4V3YJ3FAP7FP2K";
        public const string TvdbBaseUrl = "https://www.thetvdb.com/";
        public const string BannerUrl = TvdbBaseUrl + "banners/";

        public static ImageType GetImageTypeFromKeyType(string keyType)
        {
            switch (keyType.ToLowerInvariant())
            {
                case "poster":
                case "season": return ImageType.Primary;
                case "series":
                case "seasonwide": return ImageType.Banner;
                case "fanart": return ImageType.Backdrop;
                default: throw new ArgumentException($"Invalid or unknown keytype: {keyType}", nameof(keyType));
            }
        }

        public static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return null;
            }

            // pt-br is just pt to tvdb
            return language.Split('-')[0].ToLowerInvariant();
        }
    }
}
