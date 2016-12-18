using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Class BaseExtensions
    /// </summary>
    public static class StringExtensions
    {
        public static ILocalizationManager LocalizationManager { get; set; }

        public static string RemoveDiacritics(this string text)
        {
            return LocalizationManager.RemoveDiacritics(text);
        }
    }
}
