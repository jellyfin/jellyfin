using System.Globalization;

namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Isolating these helpers allow this entire project to be easily converted to Java
    /// </summary>
    public static class DoubleHelper
    {
        /// <summary>
        /// Tries the parse culture invariant.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool TryParseCultureInvariant(string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}
