
namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Class ModelExtensions
    /// </summary>
    static class ModelExtensions
    {
        /// <summary>
        /// Values the or default.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="def">The def.</param>
        /// <returns>System.String.</returns>
        public static string ValueOrDefault(this string str, string def = "")
        {
            return string.IsNullOrEmpty(str) ? def : str;
        }
    }
}
