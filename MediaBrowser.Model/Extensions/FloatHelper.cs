using System.Globalization;

namespace MediaBrowser.Model.Extensions
{
    public static class FloatHelper
    {
        /// <summary>
        /// Tries the parse culture invariant.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool TryParseCultureInvariant(string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}